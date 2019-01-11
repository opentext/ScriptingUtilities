using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DOKuStar.Stats;
using DOKuStar.Data.Xml;
using DOKuStar.Runtime.Sitemap;
using DOKuStar.Runtime.ScriptingHost;

namespace LicenseCounting
{
    public class LicenseCountingProfileExtension : Captaris.Services.BaseCustomProfileExtension
    {
        #region Interface functions

        public override XmlDocument transform(XmlDocument data, IParameters parameters)
        {
            base.transform(data, parameters);
            DataPool pool = new DataPool(data);

            switch (ScriptingManager.GetScriptName(pool))
            {
                case ScriptingManager.prepareImportName: return setGUID(pool);
                case ScriptingManager.importName: break;
                case ScriptingManager.separationName: break;
                case ScriptingManager.classificationName: break;
                case ScriptingManager.indexingName: break;
                case ScriptingManager.exportName: break;
                case ScriptingManager.afterPDFGenerationName: break;
                case ScriptingManager.afterExportName: return countLicenses(pool);
            }
            return data;
        }
        #endregion

        #region License Counting
        private const string recoGuidAnnotationName = "RecoGUID";
        private const string licenseTicketInfoAnnotationName = "LicenseTicketInfo";

        private XmlDocument setGUID(DataPool pool)
        {
            pool.RootNode.Annotations.Add(new Annotation(pool,
                recoGuidAnnotationName, Guid.NewGuid().ToString()));
            return pool.XmlDocument;
        }

        private XmlDocument countLicenses(DataPool pool)
        {
            if (!this.ReportingHelper.IsReportingActivated) return pool.XmlDocument;

            Annotation licenseInfo = pool.RootNode.Annotations[licenseTicketInfoAnnotationName];
            if (licenseInfo == null || licenseInfo.Value == String.Empty) return pool.XmlDocument;

            foreach (string lic in licenseInfo.Value.Split(';'))
            {
                if (String.IsNullOrEmpty(lic)) continue; // trailing semicolon
                string licenseName = lic.Trim().Split('=')[0].Trim();
                int pageCount = (int)System.Int64.Parse(lic.Trim().Split('=')[1].Trim());

                StatItemWriterSetting.ServiceUrl = this.ReportingHelper.ReportingUrl;
                using (StatItemWriter<OCCCustomReportItem> w = new StatItemWriter<OCCCustomReportItem>())
                {
                    w.Data.BatchId = GetBatchId(pool);
                    w.Data.BatchName = GetBatchName(pool);
                    w.Data.Profile = GetProfileName(pool);
                    w.Data.PageCount = pageCount;
                    w.Data.LicenseName = licenseName;
                    w.Data.RecoGUID = pool.RootNode.Annotations[recoGuidAnnotationName].Value;
                    w.Write(DOKuStar.Stats.JobState.OK);
                }
            }
            return pool.XmlDocument;
        }
        #endregion

        #region Table definition
        /* Definition of the SQL table and OCC proxy class

CREATE TABLE [dbo].[TB_OCCLicenseCount](
 [BatchId] [int] NULL,
 [BatchName] [nvarchar](255) NULL,
 [Profile] [nvarchar](255) NULL,
 [PageCount] [int] NULL,
 [LicenseName] [nvarchar](255) NULL,
 [BeginTime] [datetime] NULL,
 [EndTime] [datetime] NULL,
 [Duration] [int] NULL,
 [State] [int] NULL,
 [RecoGUID] [nvarchar](255) NULL
)
         */

        // Proxy class
        [TableName("TB_OCCLicenseCount")]
        public class OCCCustomReportItem : StatItem
        {
            public int BatchId { get; set; }
            public string BatchName { get; set; }
            public string Profile { get; set; }
            public int PageCount { get; set; }
            public string LicenseName { get; set; }
            public string RecoGUID { get; set; }
            protected override void Check() { base.Check(); }
        }
        #endregion

        #region Utilities
        public string GetBatchName(DataPool pool)
        {
            DOKuStar.Data.Xml.IField batchNameField = pool.RootNode.Fields["cc_BatchDetails"];
            return batchNameField == null ? string.Empty : batchNameField.Value;
        }
        public int GetBatchId(DataPool pool)
        {
            DOKuStar.Data.Xml.Int64 batchIdField = pool.RootNode.Fields["cc_BatchId"] as DOKuStar.Data.Xml.Int64;
            return batchIdField == null ? 0 : (int)batchIdField.GetValue();
        }
        public string GetProfileName(DataPool pool)
        {
            DOKuStar.Data.Xml.IField profileNameField = pool.RootNode.Fields["cc_ProfileName"];
            return profileNameField == null ? string.Empty : profileNameField.Value;
        }
        #endregion
    }
}
