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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiCAD_DB_Editor
{
    /// <summary>
    /// Interaction logic for UserControl_Category.xaml
    /// </summary>
    public partial class UserControl_Category : UserControl
    {
        private Category? _category = null;
        private Category? Category
        {
            get { return _category; }
            set { if (_category != value) _category = value; }
        }

        public UserControl_Category()
        {
            InitializeComponent();
        }

        #region Events

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is Category dC)
            {
                Category = dC;   // Take the default data object that the XAML constructed
                                 // Be careful not to reconstruct _category, as we will lose access to the category object
                                 // that the UC has been passed

                Category.UpdateDatabaseDataTable();
            }
            else
                Category = null;
        }

        private void button_NewSymbolFieldMap_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
                Category.NewSymbolFieldMap();
        }

        private void button_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
                Category.UpdateDatabaseDataTable();
        }

        private void button_NewPart_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
                Category.NewDataBaseDataTableRow();
        }

        #endregion


        #region CommandBindings

        private void CommandBinding_Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Category is not null)
                e.CanExecute = false;
            else
            {
                e.CanExecute = true;

                e.Handled = true;
            }
        }

        private void CommandBinding_Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(Category is not null);
            if (e.Parameter is SymbolFieldMap sFM)
                Category.DeleteSymbolFieldMap(sFM);

            e.Handled = true;
        }

        #endregion
    }
}
