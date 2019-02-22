using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using System.Windows.Controls;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;
using System.Windows.Data;

namespace LookupListUpdater
{
    public class ViewModel : ModelBase
    {
        #region Construction
        private Model model;    // holds current project data

        public ViewModel(Model m, PasswordBox pb)
        {
            loginTypes = new List<LogonType>()
            {
                new LogonType() { Name = "SQL server authentication", Type = SqlTableDef.LogonType.SqlUser },
                new LogonType() { Name = "Windows authentication", Type = SqlTableDef.LogonType.WindowsUser },
            };

            passwordBox = pb;
            model = m;
            SelectedTable = SqlTableDefs[0];

            OccInstallationPath = Properties.Settings.Default.OccInstallationPath;
            IsRunning = false;
            initializeProjects();

            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != LoadedProject_name && e.PropertyName != Tablenames_name)
                    SendPropertyChanged(LoadedProject_name);
            };
        }
        #endregion

        #region Properties
        private bool isRunning;
        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; SendPropertyChanged(); }
        }

        private string occInstallationPath;
        public string OccInstallationPath
        {
            get { return occInstallationPath; }
            set { occInstallationPath = value; model.OccInstallationPath = value; SendPropertyChanged(); }
        }

        private string SqlTableDefs_name = "SqlTableDefs";
        public SqlTableDefCollection SqlTableDefs
        {
            get { return model.SqlTableDefs; }
            set { model.SqlTableDefs = value; SendPropertyChanged(); }
        }

        private SqlTableDef selectedTable;
        public SqlTableDef SelectedTable
        {
            get { return selectedTable; }
            set {
                if (value == null) return;
                selectedTable = value; SendPropertyChanged();
                try
                {
                    passwordBox.Password = selectedTable.Password;
                }
                catch { }

                RaisePropertyChanged(Instance_name);
                RaisePropertyChanged(Catalog_name);
                RaisePropertyChanged(SelectedLoginType_name);
                RaisePropertyChanged(Username_name);
                RaisePropertyChanged(Password_name);
                RaisePropertyChanged(Table_name);
                RaisePropertyChanged(OccProfile_name);
                RaisePropertyChanged(OcTablename_name);
                RaisePropertyChanged(Tablenames_name);
                RaisePropertyChanged(TablesLoaded_name);
                SelectedLoginType = LoginTypes.Where(n => n.Type == selectedTable.LoginType).First();
                Tablenames = new List<string>();
                tablesLoaded = false;
            }
        }

        private string Instance_name = "Instance";
        public string Instance
        {
            get { return SelectedTable.Instance; }
            set { SelectedTable.Instance = value; SendPropertyChanged(); }
        }
        private string Catalog_name = "Catalog";
        public string Catalog
        {
            get { return SelectedTable.Catalog; }
            set { SelectedTable.Catalog = value; SendPropertyChanged(); }
        }
        private string Username_name = "Username";
        public string Username
        {
            get { return SelectedTable.Username; }
            set { SelectedTable.Username = value; SendPropertyChanged(); }
        }
        private string Password_name = "Password";
        public string Password
        {
            get { return SelectedTable.Password; }
            set { SelectedTable.Password = value; SendPropertyChanged(); }
        }

        private string Tablenames_name = "Tablenames";
        private List<string> tablenames;
        public List<string> Tablenames
        {
            get { return tablenames; }
            set { tablenames = value; SendPropertyChanged(); }
        }

        private string TablesLoaded_name = "TablesLoaded";
        private bool tablesLoaded;
        public bool TablesLoaded
        {
            get { return tablesLoaded; }
            set { tablesLoaded = value; SendPropertyChanged(); }
        }

        private string Table_name = "Table";
        public string Table
        {
            get { return SelectedTable.Table; }
            set { SelectedTable.Table = value; SendPropertyChanged(); }
        }

        private string OccProfile_name = "OccProfile";
        public string OccProfile
        {
            get { return SelectedTable.OccProfile; }
            set { SelectedTable.OccProfile = value; SendPropertyChanged(); }
        }

        private string OcTablename_name = "OccFieldname";
        public string OccTablename
        {
            get { return SelectedTable.OccTablename; }
            set { SelectedTable.OccTablename = value; SendPropertyChanged(); }
        }
        public class LogonType
        {
            public string Name { get; set; }
            public SqlTableDef.LogonType Type { get; set; }
        }

        private List<LogonType> loginTypes;
        public List<LogonType> LoginTypes
        {
            get { return loginTypes; }
            set { loginTypes = value; SendPropertyChanged(); }
        }

        private const string SelectedLoginType_name = " SelectedLoginType";
        private LogonType selectedLoginType;
        public LogonType SelectedLoginType
        {
            get { return selectedLoginType; }
            set
            {
                selectedLoginType = value;
                SelectedTable.LoginType = selectedLoginType.Type;
                ShowUserNamePassword = SelectedTable.LoginType == SqlTableDef.LogonType.SqlUser;
                RaisePropertyChanged(showUserNamePassword_name);
                SendPropertyChanged();
            }
        }

        private string showUserNamePassword_name = "showUserNamePassword";
        private bool showUserNamePassword;
        public bool ShowUserNamePassword
        {
            get { return showUserNamePassword; }
            set { showUserNamePassword = value; SendPropertyChanged(); }
        }
        #endregion

        #region Function (project related, open, close, load, ...)
        bool projectLoaded = false;

        private string RecentProjects_name = "RecentProjects";
        List<string> recentProjects = new List<string>();
        public List<string> RecentProjects
        {
            get { return recentProjects; }
            set { recentProjects = value; SendPropertyChanged(); }
        }

        private string LoadedProject_name = "LoadedProject";
        public string LoadedProject
        {
            get { return RecentProjects.Count > 0 ? (RecentProjects.First() + (Dirty ? " (modified)" : "")) : ""; }
        }

        private void initializeProjects()
        {
            if (Properties.Settings.Default.RecentProjects != null)
                RecentProjects = Properties.Settings.Default.RecentProjects.Cast<string>().ToList();

            if (RecentProjects.Count() > 0)
                loadProject(RecentProjects.First());
        }

        private void addToRecentProjecctList(string filename)
        {
            RecentProjects = RecentProjects.Reverse<string>().ToList();
            try { RecentProjects.Remove(filename); } catch { }
            if (RecentProjects.Count >= 10) RecentProjects = RecentProjects.Skip(1).ToList();
            RecentProjects.Add(filename);
            RecentProjects = RecentProjects.Reverse<string>().ToList();
            RaisePropertyChanged(RecentProjects_name);
        }

        private void removeFromRecentProjecctList(string filename)
        {
            try { RecentProjects.Remove(filename); } catch { }
            RaisePropertyChanged(RecentProjects_name);
        }

        public bool TerminateProjects()     // ... when ending the program
        {
            if (abortOperation()) return true;

            RecentProjects = RecentProjects.Where(n => !String.IsNullOrEmpty(n)).ToList();

            StringCollection collection = new StringCollection();
            collection.AddRange(RecentProjects.ToArray());
            Properties.Settings.Default.RecentProjects = collection;
            Properties.Settings.Default.OccInstallationPath = OccInstallationPath;
            Properties.Settings.Default.Save();

            return false;
        }
        public void LoadRecentProject(object sender, ExecutedRoutedEventArgs e)
        {
            if (abortOperation()) return;
            loadProject(e.Parameter as string);
        }

        public void LoadProject(object sender, ExecutedRoutedEventArgs e) { loadProject(); }
        public void SaveProject(object sender, ExecutedRoutedEventArgs e) { saveProject(); }
        public void SaveProjectAs(object sender, ExecutedRoutedEventArgs e) { saveProjectAs(); }
        public void CloseProject(object sender, ExecutedRoutedEventArgs e) { closeProject(); }

        private void loadProject()
        {
            if (abortOperation()) return;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Filter files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
                loadProject(dlg.FileName);
        }
        private void loadProject(string filename)
        {
            try { model.LoadTables(filename); }
            catch (Exception e)
            {
                loadAndStoreErrorMessage("Project loading failed.", e);
                removeFromRecentProjecctList(filename);
                return;
            }
            SelectedTable = SqlTableDefs[0];
            projectLoaded = true;
            addToRecentProjecctList(filename);
            RaisePropertyChanged(SqlTableDefs_name);
            RaisePropertyChanged(LoadedProject_name);
        }
        private void saveProject()
        {
            if (!projectLoaded)
                saveProjectAs();
            else
                saveProject(RecentProjects.First());
        }

        private void saveProjectAs()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Filter files (*.xml)|*.xml|All files (*.*)|*.*";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
                saveProject(dlg.FileName);
        }
        private void saveProject(string filename)
        {
            try { model.SaveTables(filename); }
            catch (Exception e)
            {
                loadAndStoreErrorMessage("Project storing failed.", e);
                removeFromRecentProjecctList(filename);
                return;
            }
            addToRecentProjecctList(filename);
        }

        private void closeProject()
        {
            if (abortOperation()) return;
            model.NewTableDefs();
            RaisePropertyChanged(SqlTableDefs_name);
            RecentProjects.Insert(0, string.Empty);
            SelectedTable = SqlTableDefs[0];
            projectLoaded = false;
        }
        public bool abortOperation()
        {
            if (!Dirty) return false;
            switch (MessageBox.Show(
                        "Save changes?", "Lookup list updater",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning))
            {
                case DialogResult.Yes: saveProject(); return false;
                case DialogResult.No: return false;
                default: return true;
            }
        }
        private void loadAndStoreErrorMessage(string msg, Exception e)
        {
            MessageBox.Show(
                msg + "\n" + e.Message, "Lookup List Updater",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Password
        private PasswordBox passwordBox { get; set; }

        public void PasswordChangedHandler(PasswordBox pb)
        {
            Password = getUsecuredString(pb.SecurePassword);
        }

        private string getUsecuredString(SecureString ss)
        {
            if (ss == null) return "";
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(ss);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
        #endregion

        #region Functions
        public void AddTable(object sender, ExecutedRoutedEventArgs e)
        {
            SqlTableDefs.Add(new SqlTableDef());
            SelectedTable = SqlTableDefs[SqlTableDefs.Count() - 1];
        }

        public void DeleteTable(object sender, ExecutedRoutedEventArgs e)
        {
            int index = SqlTableDefs.IndexOf(SelectedTable);
            SqlTableDef currentTable = SelectedTable;

            SelectedTable = new SqlTableDef();     // make sure SelectedTable never gets null
            SqlTableDefs.Remove(currentTable);

            if (SqlTableDefs.Count() == 0)
                SqlTableDefs.Add(SelectedTable);
            else
            {
                if (index > SqlTableDefs.Count - 1) index = SqlTableDefs.Count - 1;
                SelectedTable = SqlTableDefs[index];
            }
        }

        public void LoadTables(object sender, ExecutedRoutedEventArgs e)
        {
            try { IsRunning = true;

                SqlClient sqlClient = model.GetSqlClient(SelectedTable);
                Tablenames = sqlClient.GetTablenames();

                if (string.IsNullOrEmpty(Table))
                    Table = Tablenames[0];
                tablesLoaded = true;
                RaisePropertyChanged(TablesLoaded_name);
            }
            catch (Exception err) { showError("Can't load tables", err); }
            finally { IsRunning = false; }
        }

        public void StoreTable(object sender, ExecutedRoutedEventArgs e)
        {
            try { IsRunning = true;

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Filter files (*.csv)|*.csv|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    model.StoreTable(SelectedTable, dlg.FileName);
                    MessageBox.Show("Done", "Lookup list update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception err) { showError("Can't store table", err); }
            finally { IsRunning = false; }
        }

        public void UpdateProfile(object sender, ExecutedRoutedEventArgs e)
        {
            try { IsRunning = true;
                string msg = null;
                bool result = model.UpdateProfile(SelectedTable.TableDefinitioName, ref msg);
                MessageBox.Show(
                    (result ? "Success" : "Failed") + "\n" + msg,
                    "Lookup list update",
                    MessageBoxButtons.OK,
                    result ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
            catch (Exception err) { showError("Can't store table", err); }
            finally { IsRunning = false; }
        }

        public void About(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Version 0.7", "About LookupListUpdate", MessageBoxButtons.OK);
        }

        private void showError (string msg, Exception e)
        {
            MessageBox.Show(
                    msg + ".\n" + e.Message,
                    "LookupListUpdater",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion
    }

    #region Value converter
    public class OneWayConverter
    {
        public object ConvertBack(object value, Type targetTypeobject, object p, CultureInfo c)
        {
            throw new NotImplementedException();
        }
    }
    [ValueConversion(typeof(bool), typeof(String))]
    public class BoolToRunningCursorConverter : OneWayConverter, IValueConverter
    {
        public object Convert(object value, System.Type targetType, object p, CultureInfo c)
        {
            return (bool)value ? "Wait" : "Arrow";
        }
    }
    #endregion 
}
