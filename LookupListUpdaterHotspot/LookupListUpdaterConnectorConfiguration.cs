using System;
using System.Collections.Generic;
using System.Text;

namespace DOKuStar.Runtime.HotFolders.Connectors.LookupListUpdaterConnectors
{
    [Serializable]
    public class LookupListUpdaterConnectorConfiguration : IConnectorConfiguration
    {
        private Guid instanceGuid;
        private ConnectorSchedule schedule = new ConnectorSchedule();

        public LookupListUpdaterConnectorConfiguration()  { }
        public LookupListUpdaterConnectorConfiguration(Guid instanceGuid)
        {
            this.instanceGuid = instanceGuid;
        }
        public string SelectedTableDefinition { get;  set; }
        public string Projectfile { get; set; }

        public string Profilename { get; set; }

        public Guid ConnectorGuid
        {
            // Same GUID as in property Guid of connector class
            get { return LookupListUpdaterConnector.ConnectorGuid; }
        }
        public bool Enabled { get; set; }

        public Guid InstanceGuid
        {
            get { return instanceGuid; }
            set { instanceGuid = value; }
        }
        public string InstanceName { get; set; }

        public string LocationName
        {
            get { return null;  }
        }
        public ConnectorSchedule Schedule
        {
            get { return schedule; }
            set { schedule = value; }
        }
        public string SiteMap { get; set; }
        public string Tag { get; set; }
    }
}
