using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            if (Properties.Settings.Default.OpenProjectPath != "" && File.Exists(Properties.Settings.Default.OpenProjectPath))
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
            if (Properties.Settings.Default.OpenProjectPath != "")
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
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is Library)
                DataObj.Project.DeleteLibrary((Library)e.Parameter);

            e.Handled = true;
        }

        #endregion

        private void menuItem_FileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
