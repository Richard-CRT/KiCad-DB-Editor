using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            string lastProjectPath = Properties.Settings.Default.LastProjectPath;
            if (lastProjectPath != "")
            {
                DataObj.Project = Project.FromFile(lastProjectPath);
            }
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (saveFileDialog.ShowDialog() == true)
                DataObj.Project.SaveToFile(saveFileDialog.FileName);

            Properties.Settings.Default.LastProjectPath = saveFileDialog.FileName;
            Properties.Settings.Default.Save();
        }

        private void button_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj";
            if (openFileDialog.ShowDialog() == true)
                DataObj.Project = Project.FromFile(openFileDialog.FileName);

            Properties.Settings.Default.LastProjectPath = openFileDialog.FileName;
            Properties.Settings.Default.Save();
        }

        private void button_Include_Click(object sender, RoutedEventArgs e)
        {
            DataObj.Project.IncludeSelectedUnincludedLibrary();
        }
    }
}
