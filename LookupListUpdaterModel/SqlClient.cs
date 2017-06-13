using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

namespace LookupListUpdater
{
    /// This class contains all functions that deal with the SQL server.
    public class SqlClient : IDisposable
    {
        #region Construction
        private SqlConnection conn = null;
        public string Tablename { get; set; }

        public SqlClient() { }
        #endregion

        #region Connection
        public void Connect(string instance, string database, string username, string password)
        {
            conn = new SqlConnection();
            conn.ConnectionString = "Data Source=" + instance + ";" +
                "Initial Catalog=" + database + ";" +
                "User id=" + username + ";" +
                "Password=" + password + ";";
            conn.Open();
        }

        public void Connect(string instance, string database)
        {
            conn = new SqlConnection();
            conn.ConnectionString = "Data Source=" + instance + ";" +
                "Initial Catalog=" + database + ";" + "Integrated Security = True;";
            conn.Open();
        }

        public void Disconnect()
        {
            conn.Dispose();
        }
        #endregion

        #region SQL introspection
        public List<string> GetTablenames()
        {
            List<string> result = new List<string>();

            DataTable schema = conn.GetSchema("Tables");

            int nameColumn = 0; // = x.Columns.Where(n => n. == ")
            foreach (DataColumn dc in schema.Columns)
            {
                if(dc.ColumnName == "TABLE_NAME") break;
                nameColumn++;
            }
            foreach (DataRow dr in schema.Rows)
                result.Add((string)dr.ItemArray[nameColumn]);

            return result;
        }

        public List<string> GetColumns()
        {
            List<string> result = new List<string>();

            SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM " + Tablename, conn);
            adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            DataTable table = new DataTable();
            adapter.Fill(table);

            foreach (DataColumn dc in table.Columns)
                result.Add(dc.ColumnName);

            return result;
        }
        #endregion

        #region Read table
        SqlDataReader sdr;
        List<string> colNames;

        public void OpenTable()
        {
           this.colNames = GetColumns();

            SqlCommand cmd = new SqlCommand(
                    "select * from \"" + Tablename + "\"", conn);
            this.sdr = cmd.ExecuteReader();
        }

        public List<string> GetNextTableRow()
        {
            if (sdr.Read())
            {
                List<string> row = new List<string>();
                foreach (string colName in colNames)
                    row.Add(sdr[colName].ToString());
                return row;
            }
            return null;
        }
        #endregion

        #region IDisposable
        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    if (conn != null)
                        try { conn.Close(); conn.Dispose(); }
                        catch { }

                _disposed = true;
            }
        }

        ~SqlClient()
        {
            Dispose(false);
        }
        #endregion
    }
}
