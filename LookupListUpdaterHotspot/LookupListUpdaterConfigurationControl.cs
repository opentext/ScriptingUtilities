using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using LookupListUpdater;

namespace DOKuStar.Runtime.HotFolders.Connectors.LookupListUpdaterConnectors
{
    public partial class LookupListUpdaterConfigurationControl : UserControl, IConfigurationControl
    {
        private LookupListUpdaterConnectorConfiguration configuration;

        public LookupListUpdaterConfigurationControl()
        {
            InitializeComponent();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IConnectorConfiguration Configuration
        {
            get {
                if (configuration == null)
                    configuration = LookupListUpdaterConnector.GetNewConfiguration();
                configuration.SelectedTableDefinition = tables_comboBox.SelectedItem as string;
                configuration.Profilename = txt_profileName.Text;
                return configuration;
            }
            set {
                configuration = (value == null) ?
                    LookupListUpdaterConnector.GetNewConfiguration() :
                    (LookupListUpdaterConnectorConfiguration)value;

                loadComboBox();
                tables_comboBox.SelectedItem = tables_comboBox.Items[0];

                foreach (var x in tables_comboBox.Items)
                    if (x.ToString() == configuration.SelectedTableDefinition)
                        tables_comboBox.SelectedItem = x;

                if (string.IsNullOrEmpty(configuration.Profilename))
                    configuration.Profilename = GetProfilenameFromHotspotSitemap(configuration.SiteMap);
                txt_profileName.Text = configuration.Profilename;
             }
        }

        private string GetProfilenameFromHotspotSitemap(string sitemap)
        {
            string result = null;
            DirectoryInfo s1 = Directory.GetParent(Path.GetDirectoryName(sitemap));
            string rdaProjectFile = Path.Combine(s1.Parent.FullName, "RdaProjects.rpj");
            string jobname = Path.GetFileName(s1.FullName);

            XElement xe = XElement.Load(rdaProjectFile);
            try
            {
                result = xe.Descendants("ProjectDescriptor")
                    .Where(n => n.Element("HomeDirectory").Value == jobname)
                    .First()
                    .Element("Name").Value;
            }
            catch { }
            return result;
        }

        private void loadComboBox()
        {
            Model model = Model.Instance;
            if (!string.IsNullOrEmpty(configuration.Projectfile))
                model.LoadTables(configuration.Projectfile);
            if (model.SqlTableDefs.Count == 0)
                throw new Exception("No table definition found");
            foreach (string name in model.SqlTableDefs.Select(n => n.Name))
                tables_comboBox.Items.Add(name);
        }

        private void btn_loadProject_Click(object sender, EventArgs e)
        {
            Model model = Model.Instance;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Filter files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
                try
                {
                    model.LoadTables(dlg.FileName);
                    configuration.Projectfile = dlg.FileName;
                    tables_comboBox.Items.Clear();
                    foreach (SqlTableDef st in model.SqlTableDefs)
                        tables_comboBox.Items.Add(st.Name);
                    if (tables_comboBox.Items.Count > 0)
                        tables_comboBox.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Loading project failes" + ex.Message,
                        "Lookup list updater", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
        }
    }
}
