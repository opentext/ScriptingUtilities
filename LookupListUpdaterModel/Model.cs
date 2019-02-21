using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Reflection;
using System.Diagnostics;

namespace LookupListUpdater
{
    /// This is the model part within the MVVM patter of the program. It holds all data
    /// of the project that is loaded into the application. 
    /// The project is simply an instance of SqlTableDefCollection.
    /// The project maintains a dirty flag.
    public class Model : ModelBase
    {
        #region Construction (Singleton)
        private static Model instance;

        public Model()
        {
            NewTableDefs();
        }

        public static Model Instance
        {
            get
            {
                if (instance == null) instance = new Model();
                return instance;
            }
        }
        #endregion

        #region Properties
        public string OccInstallationPath { get; set; } = string.Empty;

        private SqlTableDefCollection sqlTableDefs;
        public SqlTableDefCollection SqlTableDefs
        {
            get { return sqlTableDefs; }
            set { SetField(ref sqlTableDefs, value); }
        }
        #endregion

        #region Create, load and save table 
        // Create new empty model
        public void NewTableDefs()
        {
            SqlTableDefs = new SqlTableDefCollection();
            SqlTableDefs.Add(new SqlTableDef());
            Dirty = false;
        }

        // Load a model from a project file
        public void LoadTables(string filename)
        {
            SqlTableDefs = deserializeFromXmlString(
                    File.ReadAllText(filename),
                    typeof(SqlTableDefCollection)) as SqlTableDefCollection;
            Dirty = false;
        }

        // Save current model state as project file
        public void SaveTables(string filename)
        {
            File.WriteAllText(filename, serializeToXmlString(sqlTableDefs));
            Dirty = false;
        }
        #endregion

        #region Serialization
        private static string serializeToXmlString(object obj)
        {
            Encoding encoding = Encoding.UTF8;
            MemoryStream memoryStream = null;
            StreamWriter streamWriter = null;
            try
            {
                // Create XmlSerializer for this type
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                byte[] buffer;

                // Serialize via memory stram
                memoryStream = new MemoryStream();
                streamWriter = new StreamWriter(memoryStream, encoding);
                serializer.Serialize(streamWriter, obj);
                
                // Return memory stream as string
                buffer = memoryStream.ToArray();
                return encoding.GetString(buffer, 0, buffer.Length);
            }
            finally
            {
                if (streamWriter == null)
                    memoryStream.Close();
                else
                    streamWriter.Close();
            }
        }

        private static object deserializeFromXmlString(string xmlString, Type objectType)
        {
            Encoding encoding = Encoding.UTF8;
            MemoryStream memoryStream = null;
            try
            {
                // Create XmlSerializer for this type
                XmlSerializer serializer = new XmlSerializer(objectType);

                // Deserialize object
                memoryStream = new MemoryStream(encoding.GetBytes(xmlString));
                return serializer.Deserialize(memoryStream);
            }
            finally
            {
                if (memoryStream != null) memoryStream.Close();
            }
        }
        #endregion
        
        #region Functions
        // Return an SqlClient that can be used to access the SQL server
        public SqlClient GetSqlClient(SqlTableDef sqlDefinition)
        {
            SqlClient sqlClient = new SqlClient();
            if (sqlDefinition.LoginType == SqlTableDef.LogonType.SqlUser)
                sqlClient.Connect(sqlDefinition.Instance, sqlDefinition.Catalog, sqlDefinition.Username, sqlDefinition.Password);
            else
                sqlClient.Connect(sqlDefinition.Instance, sqlDefinition.Catalog);

            sqlClient.Tablename = sqlDefinition.Table;
            return sqlClient;
        }
        
        /// Store table as CSV file
        public void StoreTable(SqlTableDef table, string filename)
        {
            SqlClient sqlClient = GetSqlClient(table);
            StreamWriter fs = new StreamWriter(filename);

            string s = string.Empty;
            foreach (string colName in sqlClient.GetColumns())
            {
                if (s != string.Empty) s += "\t";
                s += "\"" + colName + "\"";
            }
            fs.WriteLine(s);

            sqlClient.OpenTable();
            while (true)
            {
                List<string> row = sqlClient.GetNextTableRow();
                if (row == null) break;
                s = string.Empty;
                foreach (string col in row)
                {
                    if (s != string.Empty) s += "\t";
                    s += "\"" + col + "\"";
                }
                fs.WriteLine(s);
            }
            fs.Close();
        }

        // Called by command line and by UI
        public bool UpdateProfile(string tablename, ref string msg)
        {
            SqlTableDef table = SqlTableDefs.Where(n => n.MyName == tablename).First();
            return UpdateProfile(table, table.OccProfile, table.OccTablename, ref msg);
        }

        // Called by hotspot
        public bool UpdateProfile(string tablename, string profilename, ref string msg)
        {
            SqlTableDef table = SqlTableDefs.Where(n => n.MyName == tablename).First();
            return UpdateProfile(table, profilename, table.OccTablename, ref msg);
        }

        private bool UpdateProfile(SqlTableDef table, string occProfilename, string occFieldname, ref string msg)
        {
            string tmpName = Path.GetTempFileName();
            StoreTable(table, tmpName);

            string occFolder = string.Empty;
            string assemblyname = Assembly.GetExecutingAssembly().GetName().Name;
            if (assemblyname.Contains("LookupList") && !String.IsNullOrEmpty(OccInstallationPath))
                occFolder = OccInstallationPath;
            else {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                occFolder = Path.GetDirectoryName(path);
            }

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(occFolder, "LookupDatabaseUpdater.exe"),
                    Arguments =
                        "/server:localhost" +
                        " /profile:" + occProfilename +
                        " /table:" + occFieldname +
                        " /file:" + tmpName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            msg = string.Empty;
            try
            {
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                    msg += proc.StandardOutput.ReadLine() + "\n";

                File.Delete(tmpName);
                return proc.ExitCode == 0;
            }
            catch (Exception e)
            {
                throw new Exception($"Launching {proc.StartInfo. FileName} {proc.StartInfo.Arguments} failed. \n{e.Message}");
            }
        }
        #endregion
    }

    #region SQL table collection type definition
    /// This collection holds SQL table definitions and ensures that each such table definition
    /// is back chained to this list. 
    public class SqlTableDefCollection : ObservableCollection<SqlTableDef>
    {
        public new void Add(SqlTableDef t)
        {
            base.Add(t);
            t.SqlTables = this;
        }
    }
    #endregion

    #region SQL table definition
    /// This class holds the data that defines an SQL table, e.g. instance name, table name
    /// and login credentials.
    [Serializable]
    public class SqlTableDef : ModelBase
    {
        #region Construction
        [XmlIgnore]
        public SqlTableDefCollection SqlTables { get; set; }
        private const string initalName = "<Undefined>";

        public SqlTableDef()
        {
            MyName = initalName;
            Password = string.Empty;
            LoginType = LogonType.SqlUser;
        }
        #endregion

        #region Properties
        private string myNname;
        public string MyName
        {
            get { return myNname; }
            set { verifyUniqueness(value); SetField(ref myNname, value); }
        }

        private string instance;
        public string Instance
        {
            get { return instance; }
            set { SetField(ref instance, value); }
        }

        private string catalog;
        public string Catalog
        {
            get { return catalog; }
            set { SetField(ref catalog, value); }
        }

        public enum LogonType { SqlUser, WindowsUser };
        private LogonType loginType;
        public LogonType LoginType
        {
            get { return loginType; }
            set { SetField(ref loginType, value); }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set { SetField(ref username, value); }
        }

        private string password;
        public string Password
        {
            get { return PasswordEncryption.Decrypt(password); }
            set { SetField(ref password, PasswordEncryption.Encrypt(value)); ; }
        }

        private string table;
        public string Table
        {
            get { return table; }
            set { SetField(ref table, value); ; }
        }

        private string occProfile;
        public string OccProfile
        {
            get { return occProfile; }
            set { SetField(ref occProfile, value); ; }
        }

        private string occTablename;
        public string OccTablename
        {
            get { return occTablename; }
            set { SetField(ref occTablename, value); ; }
        }
        #endregion

        #region Functions
        private void verifyUniqueness(string newName)
        {
            if (SqlTables == null) return;
            foreach (SqlTableDef t in SqlTables)
            {
                if (this != t && newName != initalName && newName == t.MyName)
                    throw new Exception("Duplicate name");
            }
        }
        #endregion
    }
    #endregion
}
