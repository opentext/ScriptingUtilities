using System;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.InteropServices;

namespace LookupListUpdater
{
    public partial class App : Application
    {
        /// The startup function allows to use the WPF application as console program.
        /// If used as console program it needs two parameters, the name of the project 
        /// file and the name of the table within this project file. It will update the
        /// profile with the external data base data.
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 2)
            {
                // Run as console program
                AttachConsole(-1);
                Model m = Model.Instance;
                m.LoadTables(e.Args[0]);    // load project file

                string msg = string.Empty;
                bool result = m.UpdateProfile(e.Args[1], ref msg);  // do the update
                Console.Write(msg);
                Environment.ExitCode = result ? 0 : 1;
            }
            else
            {
                // Run as Windows application
                new MainWindow().ShowDialog();
            }
            this.Shutdown();
        }
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);
    }

}
