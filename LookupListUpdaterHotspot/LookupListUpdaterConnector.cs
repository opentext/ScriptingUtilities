using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using DOKuStar.Data.Xml;
using LookupListUpdater;

namespace DOKuStar.Runtime.HotFolders.Connectors.LookupListUpdaterConnectors
{
    public class LookupListUpdaterConnector : IConnector
    {
        // If you create your own connector class on the base of this
        // example, be sure to use a new GUID!
        internal static readonly Guid ConnectorGuid = new Guid("{ff997fbe-2c57-4a74-9c71-403878920cf9}");
        private LookupListUpdaterConnectorConfiguration configuration;
        
        // Auxiliary method
        internal static LookupListUpdaterConnectorConfiguration GetNewConfiguration()
        {
            return new LookupListUpdaterConnectorConfiguration(Guid.NewGuid());
        }

        public Type ConfigurationControlType
        {
            get { return typeof(LookupListUpdaterConfigurationControl); }
        }

        public string DisplayName
        {
            get { return "Lookup list updater"; }
        }

        public Guid Guid
        {
            // Same GUID as in property ConnectorGuid of configuration class
            get { return ConnectorGuid; }
        }

        public bool NeedFinishedInformations
        {
            get { return false; }
        }

        public bool NewInstanceEnabled
        {
            get { return true; }
        }

        public void CheckConfiguration(IConnectorConfiguration configuration)
        {
            LookupListUpdaterConnectorConfiguration conf = (LookupListUpdaterConnectorConfiguration) configuration;
            if (string.IsNullOrEmpty(conf.SelectedTableDefinition))
                throw new ArgumentOutOfRangeException(String.Format("No table defined"));
        }

        public IConnectorConfiguration GetCurrentConfiguration()
        {
            return this.configuration;
        }

        // Do the update
        public string[] GetCurrentDocuments()
        {
            Model model = Model.Instance;
            model.LoadTables(configuration.Projectfile);
            SqlTableDef table = model.SqlTableDefs.Where(n => n.MyName == configuration.SelectedTableDefinition).First();

            string msg = string.Empty;
            if (!model.UpdateProfile(table.MyName, configuration.Profilename, ref msg))
                throw new Exception("Update failed.\n" + msg);
   
            return new string[0];
        }

        public object GetDocument(string documentId)
        {
            return new DataPool();
        }

        public int GetDocumentCount()
        {
            return 0;
        }

        public IConnectorConfiguration GetEmptyConfiguration()
        {
            return LookupListUpdaterConnector.GetNewConfiguration();
        }

        public byte[] GetIcon()
        {
            Bitmap theBitmap = new Bitmap(Properties.Resources.data_ok, 32, 32);
            IntPtr Hicon = theBitmap.GetHicon();    // Get an Hicon for myBitmap.
            Icon icon = Icon.FromHandle(Hicon);     // Create a new icon from the handle.

            MemoryStream ms = new MemoryStream();
            icon.Save(ms);
            return ms.GetBuffer();
        }

        public void OnFinished(FinishedArgs args)
        {
            // Reserved method; for internal use. Do nothing.
        }

        public void RemoveDocument(string documentId)
        {
        }

        public void SetConfiguration(IConnectorConfiguration configuration)
        {
            this.configuration = (LookupListUpdaterConnectorConfiguration)configuration;
        }
    }
}
