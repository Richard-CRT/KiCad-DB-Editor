using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiCAD_DB_Editor
{
    /// <summary>
    /// Interaction logic for UserControl_Library.xaml
    /// </summary>
    public partial class UserControl_Library : UserControl
    {
        public static RoutedCommand ImportCommand = new RoutedCommand();
        public static RoutedCommand ExportCommand = new RoutedCommand();

        private Library? _library = null;
        private Library Library
        {
            get { Debug.Assert(_library is not null); return _library; }
            set { if (_library != value) _library = value; }
        }

        public UserControl_Library()
        {
            InitializeComponent();
        }

        #region Events

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is Library)
            {
                Library = (Library)DataContext; // Take the default data object that the XAML constructed
                                                // Be careful not to reconstruct _library, as we will lose access to the library object
                                                // that the UC has been passed
            }
        }

        private void button_NewCategory_Click(object sender, RoutedEventArgs e)
        {
            Library.NewCategory();
        }

        #endregion


        #region CommandBindings

        private void CommandBinding_Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is Category)
                Library.DeleteCategory((Category)e.Parameter);

            e.Handled = true;
        }

        private void CommandBinding_Import_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Import_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Title = "Import KiCAD DB Config File";
            openFileDialog.Filter = "KiCAD DB config file (*.kicad_dbl)|*.kicad_dbl";
            if (openFileDialog.ShowDialog() == true)
                Library.ImportFromKiCADDBL(openFileDialog.FileName);

            e.Handled = true;
        }

        private void CommandBinding_Export_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;

            e.Handled = true;
        }

        private void CommandBinding_Export_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Title = "Export KiCAD DB Config File";
            saveFileDialog.Filter = "KiCAD DB config file (*.kicad_dbl)|*.kicad_dbl";
            if (saveFileDialog.ShowDialog() == true)
                Library.ExportToKiCADDBLFile(saveFileDialog.FileName);

            e.Handled = true;
        }

        #endregion
    }
}
