using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KiCAD_DB_Editor.View
{
    /// <summary>
    /// Interaction logic for Window_EditSubLibrary.xaml
    /// </summary>
    public partial class Window_EditSubLibrary : Window
    {
        #region Dependency Properties

        public static readonly DependencyProperty SubLibraryNameProperty = DependencyProperty.Register(
            nameof(SubLibraryName),
            typeof(string),
            typeof(Window_EditSubLibrary)
            );

        public string SubLibraryName
        {
            get => (string)GetValue(SubLibraryNameProperty);
            set => SetValue(SubLibraryNameProperty, value);
        }

        #endregion

        public Window_EditSubLibrary(string subLibraryName)
        {
            InitializeComponent();

            SubLibraryName = subLibraryName;
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
