using DOKuStar.Data.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DOKuStar.Runtime.ScriptingHost;
using System.IO;

namespace ScriptingUtilities
{
    public class PreventBrokenBatchesProfileExtension : ProfileExtension_Transform
    {
        #region Custom extension functions
        // This exception is raised internally to divert the batch to external exception processing
        private const string preventBrokenBatchException = "PreventBrokenBatch: intended expection";

        // In this dictionary we store the folder where the batch should be stored in case of 
        // external exception handling
        private static Dictionary<string, string> exceptionFolder = new Dictionary<string, string>();

        protected override DataPool PrepareImport(DataPool pool)
        {
            // Store the location for external exception handling
            Annotation a = pool.RootNode.Annotations["PreventBrokenBatches_ExceptionFolder"];
            if (a != null)
                exceptionFolder[pool.RootNode.Annotations["BatchId"].Value] = a.Value;

            // Handling based on state
            switch (GetState(pool))
            {
                case PreventBrokenBatchState.None:              // never seen that batch
                    SetState(pool, PreventBrokenBatchState.New);
                    break;

                case PreventBrokenBatchState.New:               // didn't pass splitting
                case PreventBrokenBatchState.SkipOcr:           // didn't pass even without OCR
                    handleUnprocessableBatch(pool);             // store for external exception handling
                    throw new Exception(preventBrokenBatchException);
            }
            return pool;
        }

        protected override DataPool Import(DataPool pool)
        {
            switch (GetState(pool))
            {
                // When we arrive here with state "New" it's still first pass for the batch
                // but we know it could be splitted
                case PreventBrokenBatchState.New: 
                    SetState(pool, PreventBrokenBatchState.SplittingOk);
                    break;

                // When we arrive here with state "SplittingOk" we're in pass two and
                // we try it without OCR
                case PreventBrokenBatchState.SplittingOk: 
                    setBatchToSkipOcr(pool);
                    SetState(pool, PreventBrokenBatchState.SkipOcr);
                    break;
            }
            return pool;
        }

        protected override DataPool Export(DataPool pool)
        {
            // Clean up things
            DeleteHistory(pool);
            exceptionFolder.Remove(pool.RootNode.Annotations["BatchId"].Value);
            return pool;
        }
        protected override DataPool RecognitionException(DataPool pool)
        {
            // Check whether it is our own exception
            Annotation a = pool.RootNode.Annotations["WorkflowMessage"];
            if (a == null || a.Value.IndexOf(preventBrokenBatchException) != 0)
                return pool; // Let OCC handle the exception

            // We came here because of our own intention we clean up and remove the batch
            DeleteHistory(pool);
            pool.ForwardToTerminate();
            return pool;
        }

        private void setBatchToSkipOcr(DataPool pool)
        {
            // In case of loose pages input
            foreach (Document doc in pool.RootNode.Documents)
                foreach (Source s in doc.Sources)
                    s.PageInstance.SkipOcr = true;

            // In case of incoming documents
            foreach (Source s in pool.RootNode.Sources)
                if (s.PageInstance != null)
                    s.PageInstance.SkipOcr = true;
        }
        #endregion

        #region State handling
        // The state of a batch is stored in a so-called history file.
        // The file is located the batch's repository alonside the other files belonging to the batch
        public enum PreventBrokenBatchState {
            None,           // Batch has just arrived, nothing has happend yet
            New,            // This batch has been registered
            SplittingOk,    // Batch passed splitting process
            SkipOcr         // Batch is in second try, without OCR data
        };

        public PreventBrokenBatchState GetState(DataPool pool)
        {
            string filename = getHistoryFile(pool);
            if (!File.Exists(filename))
                return PreventBrokenBatchState.None;
            else
                return (PreventBrokenBatchState)Enum.Parse(typeof(PreventBrokenBatchState), File.ReadAllText(filename));
        }

        public void SetState(DataPool pool, PreventBrokenBatchState state)
        {
            File.WriteAllText(getHistoryFile(pool), state.ToString());
        }

        public void DeleteHistory(DataPool pool)
        {
            File.Delete(getHistoryFile(pool));
            batch2repository.Remove(pool.RootNode.Annotations["BatchId"].Value);
        }

        // Cache to avoid calculating the repository location again and again
        private Dictionary<string, string> batch2repository = new Dictionary<string, string>();

        private string getHistoryFile(DataPool pool)
        {
            string batchId = pool.RootNode.Annotations["BatchId"].Value;
            if (! batch2repository.ContainsKey(batchId))
                batch2repository[batchId] = getRepositoryFolder(pool);
            return Path.Combine(batch2repository[batchId], $"{batchId}_history.txt");
        }
    
        private string getRepositoryFolder(DataPool pool)
        {
            List<string> sourceFiles = new List<string>();
            foreach (SourceInstance s in pool.RootNode.GetAllSourceInstances())
                sourceFiles.Add(Path.GetDirectoryName(s.Url));

            if (sourceFiles.Distinct().Count() > 1)
                throw new Exception("More than one sources folder");

            if (sourceFiles.Distinct().Count() == 0)
                throw new Exception("No sources in batch");

            return sourceFiles.First();
        }
        #endregion

        #region Profile specific functions
        private void handleUnprocessableBatch(DataPool pool)
        {
            string batchId = pool.RootNode.Annotations["BatchId"].Value;
            string folder = @"c:\temp"; // some default
            if (exceptionFolder.ContainsKey(batchId))
            {
                folder = exceptionFolder[batchId];
                exceptionFolder.Remove(batchId);
            }
            string batchFolder = Path.Combine(folder, batchId);
            Directory.CreateDirectory(batchFolder);

            foreach (SourceInstance si in pool.RootNode.GetAllSourceInstances())
            {
                string url = Path.GetFileName(si.Url);
                string filename = Path.Combine(Path.GetDirectoryName(url), si.Id + "_" + Path.GetFileName(url));
                File.Copy(si.Url, Path.Combine(batchFolder, filename), true);
            }
            pool.Save(Path.Combine(batchFolder, batchId + ".data"));
        }
        #endregion
    }
}
