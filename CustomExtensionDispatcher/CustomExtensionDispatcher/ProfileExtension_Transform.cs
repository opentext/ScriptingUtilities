using System;
using System.Xml;
using DOKuStar.Data.Xml;
using DOKuStar.Runtime.ScriptingHost;
using DOKuStar.Runtime.Sitemap;

namespace ScriptingUtilities
{
    public class ProfileExtension_Transform : Captaris.Services.BaseCustomProfileExtension
    {
        protected virtual DataPool PrepareImport(DataPool pool) { return pool; }
        protected virtual DataPool Import(DataPool pool) { return pool; }
        protected virtual DataPool Separation(DataPool pool) { return pool; }
        protected virtual DataPool Classification(DataPool pool) { return pool; }
        protected virtual DataPool Indexing(DataPool pool) { return pool; }
        protected virtual DataPool Export(DataPool pool) { return pool; }
        protected virtual DataPool AfterPdfGeneration(DataPool pool) { return pool; }
        protected virtual DataPool AfterExport(DataPool pool) { return pool; }
        protected virtual DataPool RecognitionException(DataPool pool) { return pool; }

        public override XmlDocument transform(XmlDocument data, IParameters parameters)
        {
            base.transform(data, parameters);
            DataPool pool = new DataPool(data);

            switch (ScriptingManager.GetScriptName(pool))
            {
                case ScriptingManager.prepareImportName: return PrepareImport(pool).XmlDocument;
                case ScriptingManager.importName: return Import(pool).XmlDocument;
                case ScriptingManager.separationName: return Separation(pool).XmlDocument;
                case ScriptingManager.classificationName: return Classification(pool).XmlDocument;
                case ScriptingManager.indexingName: return Indexing(pool).XmlDocument;
                case ScriptingManager.exportName: return Export(pool).XmlDocument;
                case ScriptingManager.afterPDFGenerationName: return AfterPdfGeneration(pool).XmlDocument;
                case ScriptingManager.afterExportName: return AfterExport(pool).XmlDocument;
                case ScriptingManager.mainRecognitionExceptionName: return RecognitionException(pool).XmlDocument;
            }
            return pool.XmlDocument;
        }
    }
}
