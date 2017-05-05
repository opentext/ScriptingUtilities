using System;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.InteropServices;

namespace LookupListUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 2)
            {
                AttachConsole(-1);
                Model m = Model.Instance;
                m.LoadTables(e.Args[0]);

                string msg = string.Empty;
                bool result = m.UpdateProfile(e.Args[1], ref msg);
                Console.Write(msg);
                Environment.ExitCode = result ? 0 : 1;
            }
            else
            {
                new MainWindow().ShowDialog();
            }
            this.Shutdown();
        }
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
    }

}
