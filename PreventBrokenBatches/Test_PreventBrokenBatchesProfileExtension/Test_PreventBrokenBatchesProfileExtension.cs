using System;
using System.IO;
using DOKuStar.Data.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScriptingUtilities
{
    [TestClass]
    public class Test_PreventBrokenBatchesProfileExtension
    {
        [TestMethod]
        public void PreventBrokenBatch()
        {
            string testFolder = @"c:\temp\PBB";
            Directory.CreateDirectory(testFolder);
            string dummyFile = Path.Combine(testFolder, "a.txt");
            File.WriteAllText(dummyFile, "Hello World!");

            PreventBrokenBatchesProfileExtension extension = new PreventBrokenBatchesProfileExtension();
            DataPool pool = new DataPool();
            pool.RootNode.Annotations.Add(new Annotation(pool, "BatchId", "55"));
            pool.RootNode.Sources.Add(new Source(pool, dummyFile));

            Assert.AreEqual(PreventBrokenBatchesProfileExtension.PreventBrokenBatchState.None, extension.GetState(pool));
            extension.SetState(pool, PreventBrokenBatchesProfileExtension.PreventBrokenBatchState.SplittingOk);
            Assert.AreEqual(PreventBrokenBatchesProfileExtension.PreventBrokenBatchState.SplittingOk, extension.GetState(pool));
            extension.DeleteHistory(pool);
            Assert.AreEqual(PreventBrokenBatchesProfileExtension.PreventBrokenBatchState.None, extension.GetState(pool));

            File.Delete(dummyFile);
            Directory.Delete(testFolder);
        }
    }
}
