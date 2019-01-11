using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Windows.Controls;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows;
using System.IO;
using LicenseCounting;

namespace LicenseReport
{
    public class ViewModel : ModelBase
    {
        #region Construction
        private SqlClient sqlClient;

        public ViewModel(PasswordBox pb)
        {
            LoginType = LoginVariant.Windows;
            LoginVariants = new Dictionary<string, LoginVariant>();
            foreach (LoginVariant lv in Enum.GetValues(typeof(LoginVariant)).Cast<LoginVariant>())
                LoginVariants.Add(lv.ToString(), lv);

            TillDate = DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            StartDate = DateTime.Now.AddMonths(-1);

            sqlClient = new SqlClient();
        }
        #endregion

        #region Properties
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

        public enum LoginVariant { Windows, SQL }
        private LoginVariant loginType;
        public LoginVariant LoginType
        {
            get { return loginType; }
            set
            {
                SetField(ref loginType, value);
                SendPropertyChanged("CredentialsVisibility");
            }
        }
        private Dictionary<string, LoginVariant> loginVariants;
        public Dictionary<string, LoginVariant> LoginVariants
        {
            get { return loginVariants; }
            set { SetField(ref loginVariants, value); }
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

        private bool savePassword;
        public bool SavePassword
        {
            get { return savePassword; }
            set { SetField(ref savePassword, value); ; }
        }

        public bool CredentialsVisibility
        {
            get { return LoginType == LoginVariant.SQL; }
        }

        private int timeout;
        public int Timeout
        {
            get { return timeout; }
            set { SetField(ref timeout, value); ; }
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set { SetField(ref startDate, value); ; }
        }


        private DateTime tillDate;
        public DateTime TillDate
        {
            get { return tillDate; }
            set { SetField(ref tillDate, value.Date.AddHours(23).AddMinutes(59).AddSeconds(59)); }
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
        public void TestConnection()
        {
            try
            {
                DateTime start = DateTime.Now;
                connect();
                TimeSpan ts = DateTime.Now - start;
                MessageBox.Show($"Connection established.\n{ts.TotalMilliseconds.ToString()} milliesconds", "License report");
            }
            catch (Exception e) { showError(e.Message); }
        }

        private void connect()
        {
            sqlClient.Timeout = Timeout;
            sqlClient.Tablename = "TB_OCCLicenseCount";

            if (LoginType == LoginVariant.Windows)
                sqlClient.Connect(Instance, Catalog);
            else
                sqlClient.Connect(Instance, Catalog, Username, Password);
        }

        private void showError(string msg)
        {
            MessageBox.Show(msg, "License report", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void CreateReport()
        {
            List<SqlClient.LicenseUse> licenseUse;
            try
            {
                connect();
                licenseUse = sqlClient.ReadTable(StartDate, TillDate);
                licenseUse = accumulate(licenseUse);
                createExcelFile(licenseUse);
            }
            catch (Exception e) { showError(e.Message); }
        }

        public void DeleteData()
        {
            List<SqlClient.LicenseUse> licenseUse;
            try
            {
                connect();
                licenseUse = sqlClient.DeleteData(StartDate, TillDate);
                MessageBox.Show("Done.", "License report", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e) { showError(e.Message); }
        }

        private List<SqlClient.LicenseUse> accumulate(List<SqlClient.LicenseUse> rawlist)
        {
            List<SqlClient.LicenseUse> result = new List<SqlClient.LicenseUse>();
            foreach (SqlClient.LicenseUse lu in rawlist)
            {
                SqlClient.LicenseUse tmp = result.Where(n => n.Profile == lu.Profile && n.License == lu.License).FirstOrDefault();
                if (tmp == null)
                    result.Add(lu);
                else
                    tmp.Count += lu.Count;
            }
            return result;
        }

        private void createExcelFile(List<SqlClient.LicenseUse> accumulatedList)
        {
            string filename = Path.GetTempFileName();
            filename = Path.ChangeExtension(filename, ".xlsx");
            File.WriteAllBytes(filename, Properties.Resources.ExcelTemplate);

            Microsoft.Office.Interop.Excel.Application excel;
            Microsoft.Office.Interop.Excel.Workbook workbook;
            Microsoft.Office.Interop.Excel.Worksheet worksheet;

            excel = new Microsoft.Office.Interop.Excel.Application();
            excel.Visible = false;
            excel.DisplayAlerts = false;

            workbook = excel.Workbooks.Open(Path.GetFullPath(filename));

            try
            {
                Microsoft.Office.Interop.Excel.Sheets excelSheets = workbook.Worksheets;
                worksheet = excelSheets.get_Item(1) as Microsoft.Office.Interop.Excel.Worksheet;

                int row = 1;
                foreach (SqlClient.LicenseUse lu in accumulatedList.OrderBy(n => n.Profile).ThenBy(n => n.License))
                {
                    row++;
                    worksheet.Cells[row, 1] = lu.Profile;
                    worksheet.Cells[row, 2] = lu.License;
                    worksheet.Cells[row, 3] = lu.Count;
                }
                workbook.Save();
            }
            catch (Exception e)
            {
                try { File.Delete(filename); } catch { }
                throw e;
            }
            finally
            {
                workbook.Close();
                excel.Quit();
            }

            System.Diagnostics.Process.Start(filename);
        }
        #endregion
    }

    #region Model base
    public class ModelBase : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            RaisePropertyChanged(propertyName);
        }

        protected void SendPropertyChanged([CallerMemberName]string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        public void RaisePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
    #endregion
}
