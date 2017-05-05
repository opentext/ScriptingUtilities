using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

using DOKuStar.Data.Xml;
using DOKuStar.Diagnostics.Tracing;

namespace ScriptingUtilities
{
    #region ScriptingUtilities: Static methods
    public partial class ScriptingUtilities
    {
        // Names of the OCC annotations used to communicat to the profile extension
        public const string SqlVerificationSpecsAnnotationName = "SU_SqlVerificationSpecs";
        public const string SqlVerifyAnnotationName = "SU_SqlVerify";

        // Add an Sql verification specification to the annotation at the batch.
        public static void AddSqlVerificationSpec(DataPool pool, SqlVerificationSepc spec)
        {
            // Read existing annotation from batch or create new one
            List<SqlVerificationSepc> specs;
            Annotation SqlVerificationSpecAnnotation = pool.RootNode.Annotations[SqlVerificationSpecsAnnotationName];
            if (SqlVerificationSpecAnnotation == null)
                specs = new List<SqlVerificationSepc>();
            else
                specs = SIEESerializer.StringToObject(SqlVerificationSpecAnnotation.Value) as List<SqlVerificationSepc>;

            // If this annotation does not already exists add the current one
            if (specs.Where(n => n.Name == spec.Name).Count() == 0)
            {
                specs.Add(spec);
                // Create new annotation if none had been there
                if (SqlVerificationSpecAnnotation == null)
                {
                    SqlVerificationSpecAnnotation = new Annotation(pool, SqlVerificationSpecsAnnotationName);
                    pool.RootNode.Annotations.Add(SqlVerificationSpecAnnotation);
                }
                // Store all SqlSpecs at the annotation
                SqlVerificationSpecAnnotation.Value = SIEESerializer.ObjectToString(specs);
            }
        }

        public static void SqlVerify(DataPool pool, string specName)
        {
            // Set the annotation to trigger verification
            pool.RootNode.Annotations.Add(new Annotation(pool, SqlVerifyAnnotationName, specName));
        }

        public static void SqlVerify(IField field, string specName)
        {
            SqlVerify(field.OwnerDataPool, specName);
        }
    }
    #endregion

    #region SqlTableSpec: Data to connect to an SQL server
    [Serializable]
    public class SqlTableSpec
    {
        private static readonly ITrace trace = TraceManager.GetTracer(typeof(SqlTableSpec));

        // Constructors
        public SqlTableSpec(string instance, string catalog, string table)
        {
            Instance = instance;
            Catalog = catalog;
            Table = table;
            SqlAuthentication = false;
        }
        public SqlTableSpec(string instance, string catalog, string table, string username, string passowrd)
        {
            Instance = instance;
            Catalog = catalog;
            Table = table;
            SqlAuthentication = true;
            Username = username;
            Password = passowrd;
        }

        [NonSerialized]
        private SqlConnection conn = null;

        public string Instance { get; set; }
        public string Catalog { get; set; }
        public string Table { get; set; }
        public bool SqlAuthentication { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; }

        public bool Equals(SqlTableSpec sc)
        {
            return
                Instance == sc.Instance &&
                Catalog == sc.Catalog &&
                Table == sc.Table &&
                SqlAuthentication == sc.SqlAuthentication &&
                Username == sc.Username &&
                Password == sc.Password;
        }

        public override string ToString()
        {
            return
                "Instance=" + Instance +
                "Catalog=" + Catalog +
                "Table=" + Table +
                "SqlAuthentication=" + SqlAuthentication.ToString() +
                "Username=" + Username;
        }

        public void Connect()
        {
            if (conn != null) return;

            trace.WriteFine("Login" + ToString());
            conn = new SqlConnection();
            conn.ConnectionString = "Data Source=" + Instance + ";" + "Initial Catalog=" + Catalog + ";";
            if (SqlAuthentication)
                conn.ConnectionString += "User id=" + Username + ";" + "Password=" + Password + ";";
            else
                conn.ConnectionString += "Integrated Security = True;";
            conn.Open();
        }

        // Read from database and store values in the mappings
        public bool Read(string command, List<SqlFieldMapping> fields)
        {
            trace.WriteFine("Read command: " + command);
            SqlCommand cmd = new SqlCommand(command, conn);

            using (SqlDataReader sdr = cmd.ExecuteReader())
            {
                if (!sdr.Read())
                {
                    trace.WriteFine("No match");
                    return false;
                }
                foreach (SqlFieldMapping fh in fields)
                    fh.Value = sdr[fh.Column].ToString();

                if (sdr.Read())
                {
                    trace.WriteFine("More than one match");
                    return false;
                }
                return true;
            }
        }
    }
    #endregion

    #region SqlFieldMapping: Column name <--> OCC field name
    [Serializable]
    public class SqlFieldMapping
    {
        public SqlFieldMapping(string column, string occField)
        {
            Column = column;
            OCCField = occField;
        }
        public string Column { get; set; }
        public string OCCField { get; set; }
        public string Value { get; set; }
    }
    #endregion

    #region SqlVerificationSepc: All things connected
    [Serializable]
    public class SqlVerificationSepc
    {
        public SqlVerificationSepc(string name, SqlTableSpec tableSpec, string classname, List<SqlFieldMapping> fieldMap)
        {
            Name = name;
            TableSpec = tableSpec;
            Classname = classname;
            FieldMap = fieldMap;
        }
        public string Name { set; get; }
        public SqlTableSpec TableSpec;
        public string Classname { get; set; }
        public List<SqlFieldMapping> FieldMap { get; set; }
    }
    #endregion
}
