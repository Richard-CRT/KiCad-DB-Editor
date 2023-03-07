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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataObj = (DataObj)DataContext; // Take the default data object that the XAML constructed
                                            // Be careful not to reconstruct _project, as we will lose access to the project object
                                            // that the form is using

            if (Properties.Settings.Default.OpenProjectPath != "" && File.Exists(Properties.Settings.Default.OpenProjectPath))
            {
                DataObj.Project = Project.FromFile(Properties.Settings.Default.OpenProjectPath);
            }
        }

        private void button_NewLibrary_Click(object sender, RoutedEventArgs e)
        {
            DataObj.Project.NewLibrary("foodesc");
        }

        #region Menu > File

        private void menuItem_FileNewProject_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItem_FileOpenProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (openFileDialog.ShowDialog() == true)
            {
                DataObj.Project = Project.FromFile(openFileDialog.FileName);

                Properties.Settings.Default.OpenProjectPath = openFileDialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void menuItem_FileSaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.OpenProjectPath != "")
                DataObj.Project.SaveToFile(Properties.Settings.Default.OpenProjectPath);
        }

        private void menuItem_FileSaveAsProject_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (saveFileDialog.ShowDialog() == true)
            {
                DataObj.Project.SaveToFile(saveFileDialog.FileName);

                Properties.Settings.Default.OpenProjectPath = saveFileDialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void menuItem_FileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
