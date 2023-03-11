using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiCAD_DB_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand ExitCommand = new RoutedCommand();
        public static RoutedCommand ImportCommand = new RoutedCommand();
        public static RoutedCommand ExportCommand = new RoutedCommand();

        private DataObj? _dataObj = null;
        private DataObj DataObj
        {
            get { Debug.Assert(_dataObj is not null); return _dataObj; }
            set { if (_dataObj != value) _dataObj = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataObj = (DataObj)DataContext; // Take the default data object that the XAML constructed
                                            // Be careful not to reconstruct _dataObj, as we will lose access to the dataObj object
                                            // that the form is using

            if (Properties.Settings.Default.OpenProjectPath == "")
            {
                Debug.Assert(ApplicationCommands.New.CanExecute(null, this));
                ApplicationCommands.New.Execute(null, this);
            }

            if (Properties.Settings.Default.OpenProjectPath != "New Project" && File.Exists(Properties.Settings.Default.OpenProjectPath))
            {
                DataObj.Project = Project.FromFile(Properties.Settings.Default.OpenProjectPath);
            }
        }

        private void button_NewLibrary_Click(object sender, RoutedEventArgs e)
        {
            DataObj.Project.NewLibrary();
        }

        #endregion


        #region CommandBindings

        private void CommandBinding_New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataObj.Project = new();

            Properties.Settings.Default.OpenProjectPath = "New Project";

            e.Handled = true;
        }

        private void CommandBinding_Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Title = "Open KiCAD DB Editor Project File";
            openFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (openFileDialog.ShowDialog() == true)
            {
                DataObj.Project = Project.FromFile(openFileDialog.FileName);

                Properties.Settings.Default.OpenProjectPath = openFileDialog.FileName;
                Properties.Settings.Default.Save();
            }

            e.Handled = true;
        }

        private void CommandBinding_Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this); // Force LostFocus validation

            if (Properties.Settings.Default.OpenProjectPath != "" && Properties.Settings.Default.OpenProjectPath != "New Project")
            {
                DataObj.Project.SaveToFile(Properties.Settings.Default.OpenProjectPath);

                e.Handled = true;
            }
            else
                CommandBinding_SaveAs_Executed(sender, e);
        }

        private void CommandBinding_SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this); // Force LostFocus validation

            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Title = "Save KiCAD DB Editor Project File";
            saveFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (saveFileDialog.ShowDialog() == true)
            {
                DataObj.Project.SaveToFile(saveFileDialog.FileName);

                Properties.Settings.Default.OpenProjectPath = saveFileDialog.FileName;
                Properties.Settings.Default.Save();
            }

            e.Handled = true;
        }

        private void CommandBinding_Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (DataObj.Project.SelectedLibrary is null)
                e.CanExecute = false;
            else
            {
                e.CanExecute = true;

                e.Handled = true;
            }
        }

        private void CommandBinding_Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(DataObj.Project.SelectedLibrary is not null);
            DataObj.Project.DeleteLibrary(DataObj.Project.SelectedLibrary);

            e.Handled = true;
        }

        private void CommandBinding_Help_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Help_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://docs.kicad.org/7.0/en/eeschema/eeschema.html#database-libraries") { UseShellExecute = true });

            e.Handled = true;
        }

        private void CommandBinding_Exit_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Exit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();

            e.Handled = true;
        }

        private void CommandBinding_Import_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Import_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            bool? result;
            int i = 1;
            do
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Title = $"Import KiCAD DB Config File {i}";
                openFileDialog.Filter = "KiCAD DB config file (*.kicad_dbl)|*.kicad_dbl";
                result = openFileDialog.ShowDialog();
                if (result == true)
                    DataObj.Project.NewLibrary(openFileDialog.FileName);
                i++;
            }
            while (result == true);

            e.Handled = true;
        }

        private void CommandBinding_Export_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Export_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            bool? result;
            foreach (Library library in DataObj.Project.Libraries)
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Title = $"Export KiCAD DB Config File | Library \"{library.Name}\" ";
                saveFileDialog.Filter = "KiCAD DB config file (*.kicad_dbl)|*.kicad_dbl";
                result = saveFileDialog.ShowDialog();
                if (result == true)
                    library.ExportToKiCADDBLFile(saveFileDialog.FileName);
                else
                    break;
            }

            e.Handled = true;
        }

        #endregion
    }
}
