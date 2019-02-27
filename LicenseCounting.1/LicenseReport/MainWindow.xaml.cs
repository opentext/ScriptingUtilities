using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LicenseReport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel vm;

        public MainWindow()
        {
            InitializeComponent();
            PasswordBox pb = (PasswordBox)LogicalTreeHelper.FindLogicalNode(this, "passwordBox");
            vm = new ViewModel(pb);
            this.DataContext = vm;

            vm.Instance = Properties.Settings.Default.Instance;
            vm.Catalog = Properties.Settings.Default.Catalog;
            Enum.TryParse(Properties.Settings.Default.LoginType, out ViewModel.LoginVariant xx);
            vm.LoginType = xx;
            vm.Username = Properties.Settings.Default.Username;
            vm.Password = Properties.Settings.Default.Password;
            pb.Password = vm.Password;
            vm.SavePassword = Properties.Settings.Default.SavePassword;
            vm.Timeout = Properties.Settings.Default.Timeout;

            if (Properties.Settings.Default.StartDate != new DateTime())
                vm.StartDate = Properties.Settings.Default.StartDate;
            if (Properties.Settings.Default.TillDate != new DateTime())
                vm.TillDate = Properties.Settings.Default.TillDate;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Instance = vm.Instance;
            Properties.Settings.Default.Catalog = vm.Catalog;
            Properties.Settings.Default.LoginType = vm.LoginType.ToString();
            Properties.Settings.Default.Username = vm.Username;
            Properties.Settings.Default.Password = vm.Password;
            Properties.Settings.Default.SavePassword = vm.SavePassword;
            Properties.Settings.Default.Timeout = vm.Timeout;
            Properties.Settings.Default.StartDate = vm.StartDate;
            Properties.Settings.Default.TillDate = vm.TillDate;
            Properties.Settings.Default.Save();
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext == null) return;  // happens during initialization
            ((ViewModel)DataContext).PasswordChangedHandler(e.OriginalSource as PasswordBox);
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModel)DataContext).TestConnection();
        }

        private void CreateReport_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModel)DataContext).CreateReport();
        }

        private void DeleteData_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModel)DataContext).DeleteData();
        }
    }
}
