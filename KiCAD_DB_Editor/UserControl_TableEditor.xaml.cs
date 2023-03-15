using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for UserControl_TableEditor.xaml
    /// </summary>
    public partial class UserControl_TableEditor : UserControl
    {
        public static RoutedCommand DeleteColumnCommand = new RoutedCommand();

        private Category? _category = null;
        private Category? Category
        {
            get { return _category; }
            set
            {
                if (_category != value)
                {
                    if (_category is not null)
                        _category.DataTableUpdated -= _category_DataTableUpdated;
                    _category = value;

                    if (_category is not null)
                        _category.DataTableUpdated += _category_DataTableUpdated;
                }
            }
        }

        private void _category_DataTableUpdated(object? sender, EventArgs e)
        {
            dataGrid_TableEditor.AutoGenerateColumns = false;
            dataGrid_TableEditor.AutoGenerateColumns = true;
        }

        public UserControl_TableEditor()
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

                if (!Category.DatabaseConnectionValid)
                    Category.UpdateDatabaseDataTable();
            }
            else
                Category = null;
        }

        private void button_Fetch_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
                Category.UpdateDatabaseDataTable();
        }

        private void button_WriteChanges_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
                Category.WriteToDataBase();
        }

        private void button_NewPart_Click(object sender, RoutedEventArgs e)
        {
            if (Category is not null)
            {
                (string primaryKey, List<Category> failedCategories) = Category.GetNextPrimaryKey();
                if (
                    !failedCategories.Any() ||
                    MessageBox.Show(
                        Window.GetWindow(this),
                        $"The next component part number can't be fully verified because the connection to the following tables failed:{Environment.NewLine}" +
                        $"{string.Join(Environment.NewLine, failedCategories)}{Environment.NewLine}" +
                        $"{Environment.NewLine}" +
                        $"Press OK to proceed adding the part, you can still edit the part number after it has been added.",
                        "Connection Failed",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning) == MessageBoxResult.OK
                        )
                {
                    Category.NewDataBaseDataTableRow(primaryKey);
                }
            }
        }

        private DataGridColumnHeader? selectedDGCH = null;
        private void dataGrid_TableEditor_ColumnHeader_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is DataGridColumnHeader dGCH)
                selectedDGCH = dGCH;
            else
                selectedDGCH = null;
        }

        #endregion


        #region CommandBindings

        private void CommandBinding_DeleteColumn_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Category is null)
                e.CanExecute = false;
            else
            {
                e.CanExecute = true;

                e.Handled = true;
            }
        }

        private void CommandBinding_DeleteColumn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (selectedDGCH is not null && selectedDGCH.DataContext is string columnName)
            {
                Debug.Assert(Category is not null);

                Category.DeleteDataBaseDataTableColumn(columnName);

                e.Handled = true;
            }
        }

        #endregion
    }
}
