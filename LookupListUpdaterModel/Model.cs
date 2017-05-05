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
        private SqlTableDefCollection sqlTableDefs;
        public SqlTableDefCollection SqlTableDefs
        {
            get { return sqlTableDefs; }
            set { SetField(ref sqlTableDefs, value); }
        }
        #endregion

        #region Load and save table definitions
        public void NewTableDefs()
        {
            SqlTableDefs = new SqlTableDefCollection();
            SqlTableDefs.Add(new SqlTableDef());
            Dirty = false;
        }
        public void LoadTables(string filename)
        {
            SqlTableDefs = deserializeFromXmlString(
                    File.ReadAllText(filename),
                    typeof(SqlTableDefCollection)) as SqlTableDefCollection;
            Dirty = false;
        }
        public void SaveTables(string filename)
        {
            File.WriteAllText(filename, serializeToXmlString(sqlTableDefs));
            Dirty = false;
        }

        private static SqlTableDefCollection loadTables()
        {
            try
            {
                return deserializeFromXmlString(
                    File.ReadAllText(configFile()),
                    typeof(SqlTableDefCollection)) as SqlTableDefCollection;
            }
            catch { return new SqlTableDefCollection(); }
        }

        public void SaveTables()
        {
            File.WriteAllText(configFile(), serializeToXmlString(sqlTableDefs));
        }

        private static string configFile()
        {
            string configFile = Path.Combine(Path.GetDirectoryName(
                new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).PathAndQuery),
                "LookupListUpdater.exe.config");

            var str = XElement.Parse(File.ReadAllText(configFile));

            string exchangeFolder =
                str.Elements().Descendants().
                Where(n => n.Name == "setting" && (string)n.Attribute("name") == "ExchangeFolder").
                First().Value;

            return Path.Combine(exchangeFolder, "LookupListUpdate.xml");
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
                // XmlSerializer für den Typ des Objekts erzeugen
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                byte[] buffer;

                // Objekt über ein MemoryStream-Objekt serialisieren
                memoryStream = new MemoryStream();
                streamWriter = new StreamWriter(memoryStream, encoding);
                serializer.Serialize(streamWriter, obj);
                
                // MemoryStream in einen String umwandeln und diesen zurückgeben
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
                // XmlSerializer für den Typ des Objekts erzeugen
                XmlSerializer serializer = new XmlSerializer(objectType);

                // Objekt über ein MemoryStream-Objekt deserialisieren
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
        public SqlClient GetSqlClient(SqlTableDef sqlTable)
        {
            SqlClient sqlClient = new SqlClient();
            if (sqlTable.LoginType == SqlTableDef.LogonType.SqlUser)
                sqlClient.Connect(sqlTable.Instance, sqlTable.Catalog, sqlTable.Username, sqlTable.Password);
            else
                sqlClient.Connect(sqlTable.Instance, sqlTable.Catalog);

            sqlClient.Tablename = sqlTable.Table;
            return sqlClient;
        }
        
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
            SqlTableDef table = SqlTableDefs.Where(n => n.Name == tablename).First();
            return UpdateProfile(table, table.OccProfile, table.OccFieldname, ref msg);
        }

        // Called by hotspot
        public bool UpdateProfile(string tablename, string profilename, ref string msg)
        {
            SqlTableDef table = SqlTableDefs.Where(n => n.Name == tablename).First();
            return UpdateProfile(table, profilename, table.OccFieldname, ref msg);
        }

        private bool UpdateProfile(SqlTableDef table, string occProfilename, string occFieldname, ref string msg)
        {
            string tmpName = Path.GetTempFileName();
            StoreTable(table, tmpName);

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string OCCFolder =  Path.GetDirectoryName(path);

            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(OCCFolder, "LookupDatabaseUpdater.exe"),
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
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
                msg += proc.StandardOutput.ReadLine() + "\n";
 
            File.Delete(tmpName);
            return proc.ExitCode == 0;
        }
        #endregion
    }

    #region SqlTableCollection type definition
    public class SqlTableDefCollection : ObservableCollection<SqlTableDef>
    {
        public new void Add(SqlTableDef t)
        {
            base.Add(t);
            t.SqlTables = this;
        }
    }
    #endregion

    #region SqlTable
    [Serializable]
    public class SqlTableDef : ModelBase
    {
        #region Construction
        [XmlIgnore]
        public SqlTableDefCollection SqlTables { get; set; }
        private const string initalName = "<Undefined>";

        public SqlTableDef()
        {
            Name = initalName;
            Password = string.Empty;
            LoginType = LogonType.SqlUser;
        }
        #endregion

        #region Properties
        private string name;
        public string Name
        {
            get { return name; }
            set { verifyUniqueness(value); SetField(ref name, value); }
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

        private string occFieldname;
        public string OccFieldname
        {
            get { return occFieldname; }
            set { SetField(ref occFieldname, value); ; }
        }
        #endregion

        #region Functions
        private void verifyUniqueness(string newName)
        {
            if (SqlTables == null) return;
            foreach (SqlTableDef t in SqlTables)
            {
                if (this != t && newName != initalName && newName == t.Name)
                    throw new Exception("Duplicate name");
            }
        }
        #endregion
    }
    #endregion
}
