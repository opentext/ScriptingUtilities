using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace LicenseCounting
{
    public class SqlClient : IDisposable
    {
        #region Construction
        private SqlConnection conn = null;
        public string Tablename { get; set; }
        public int Timeout { get; set; } = 10;

        public SqlClient() { }
        #endregion

        #region Connection
        public void Connect(string instance, string database, string username, string password)
        {
            conn = new SqlConnection();
            conn.ConnectionString = "Data Source=" + instance + ";" +
                "Initial Catalog=" + database + ";" +
                "User id=" + username + ";" +
                "Password=" + password + ";" +
                "Timeout=" + Timeout.ToString() + ";";
            conn.Open();
        }

        public void Connect(string instance, string database)
        {
            conn = new SqlConnection();
            conn.ConnectionString = "Data Source=" + instance + ";" +
                "Initial Catalog=" + database + ";" + "Integrated Security = True;" +
                "Timeout=" + Timeout.ToString() + ";";
            conn.Open();
        }

        public void Disconnect()
        {
            conn.Dispose();
        }
        #endregion

        #region Read table
        SqlDataReader sdr;

        public List<LicenseUse> ReadTable(DateTime from, DateTime till)
        {
            List<LicenseUse> result = new List<LicenseUse>();
            SqlCommand cmd = new SqlCommand(
                    $"select distinct Profile, LicenseName, PageCount, RecoGUID from \"{Tablename}\" " +
                    $"where EndTime >= convert(datetime,'{from}') " +
                    $"and EndTime <= convert(datetime,'{till}') "
                    , conn);
            this.sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                string lic = sdr["Profile"].ToString();
                result.Add(new LicenseUse()
                {
                    Profile = (string)sdr["Profile"],
                    License = (string)sdr["LicenseName"],
                    Count = (int)sdr["PageCount"],
                });
            }
            return result;
        }

        public List<LicenseUse> DeleteData(DateTime from, DateTime till)
        {
            List<LicenseUse> result = new List<LicenseUse>();
            SqlCommand cmd = new SqlCommand(
                    $"delete from \"{Tablename}\" " +
                    $"where EndTime >= convert(datetime,'{from}') " +
                    $"and EndTime <= convert(datetime,'{till}') "
                    , conn);
            cmd.ExecuteReader();

            return result;
        }
        #endregion

        #region Interface data
        public class LicenseUse
        {
            public string Profile { get; set; }
            public string License { get; set; }
            public int Count { get; set; }
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
