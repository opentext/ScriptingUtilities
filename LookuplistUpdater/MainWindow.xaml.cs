using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace LookupListUpdater
{
    public partial class MainWindow : RibbonWindow
    {
        ViewModel vm;

        public MainWindow()
        {
            InitializeComponent();
            vm = new ViewModel(
                Model.Instance,
                (PasswordBox)LogicalTreeHelper.FindLogicalNode(this, "passwordBox"));
            this.DataContext = vm;

            this.Width = Properties.Settings.Default.WindowSize.Width;
            this.Height = Properties.Settings.Default.WindowSize.Height;
        }

        private void WindowLoaded(object sender, RoutedEventArgs args)
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, vm.LoadProject));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, vm.CloseProject));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, vm.SaveProject));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, vm.SaveProjectAs));

            CommandBindings.Add(new CommandBinding(loadTables, vm.LoadTables));
            CommandBindings.Add(new CommandBinding(ExitCommand, exit));
            CommandBindings.Add(new CommandBinding(LoadRecentProjectCommand, vm.LoadRecentProject));
            CommandBindings.Add(new CommandBinding(AddTableCommand, vm.AddTable));
            CommandBindings.Add(new CommandBinding(DeleteTableCommand, vm.DeleteTable));
            CommandBindings.Add(new CommandBinding(StoreTableCommand, vm.StoreTable));
            CommandBindings.Add(new CommandBinding(UpdateCommand, vm.UpdateProfile));
            CommandBindings.Add(new CommandBinding(AboutCommand, vm.About));
        }

        private void exit(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width,(int)this.Height);
            Properties.Settings.Default.Save();
            if (vm.TerminateProjects()) e.Cancel = true;
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext == null) return;  // happens during initialization
            ((ViewModel)DataContext).PasswordChangedHandler(e.OriginalSource as PasswordBox);
        }

        #region Commands

        private static RoutedUICommand exitCommand = new RoutedUICommand(
            "Exit", "exit", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                        new KeyGesture(Key.W, ModifierKeys.Control)
            }));
        public static RoutedUICommand ExitCommand { get { return exitCommand; } }

        private static RoutedUICommand loadRecentProjectCommand = new RoutedUICommand(
            "Load recent project", "loadRecentProject", typeof(MainWindow));
        public static RoutedUICommand LoadRecentProjectCommand { get { return loadRecentProjectCommand; } }

        private static RoutedUICommand addTableCommand = new RoutedUICommand(
            "Add table", "AddTable", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.Add, ModifierKeys.None)
            }));
        public static RoutedUICommand AddTableCommand { get { return addTableCommand; } }

        private static RoutedUICommand deleteTableCommand = new RoutedUICommand(
            "Delete table", "DeöeteTable", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.Delete, ModifierKeys.None)
            }));
        public static RoutedUICommand DeleteTableCommand { get { return deleteTableCommand; } }

        private static RoutedUICommand loadTables = new RoutedUICommand(
            "Load tables", "LoadTables", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.L, ModifierKeys.Control)
            }));
        public static RoutedUICommand LoadTables { get { return loadTables; } }

        private static RoutedUICommand storeTableCommand = new RoutedUICommand(
            "Store table as CSV file", "StoreTable", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                new KeyGesture(Key.S, ModifierKeys.Control)
            }));
        public static RoutedUICommand StoreTableCommand { get { return storeTableCommand; } }

        private static RoutedUICommand updateCommand = new RoutedUICommand(
            "Update OCC profile", "UpdateProfile", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                        new KeyGesture(Key.U, ModifierKeys.Control)
            }));
        public static RoutedUICommand UpdateCommand { get { return updateCommand; } }

        private static RoutedUICommand aboutCommand = new RoutedUICommand(
            "Show Version", "showVersion", typeof(MainWindow),
            new InputGestureCollection(new List<InputGesture>() {
                        new KeyGesture(Key.V, ModifierKeys.Control)
            }));
        public static RoutedUICommand AboutCommand { get { return aboutCommand; } }
        #endregion
    }
}
