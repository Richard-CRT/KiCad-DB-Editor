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
        public static RoutedCommand FetchCommand = new RoutedCommand();
        public static RoutedCommand WriteChangesCommand = new RoutedCommand();
        public static RoutedCommand AlterTableCommand = new RoutedCommand();

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

        #endregion


        #region CommandBindings

        private void CommandBinding_AlterTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Category is null)
                e.CanExecute = false;
            else
            {
                e.CanExecute = true;

                e.Handled = true;
            }
        }

        private void CommandBinding_AlterTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(Category is not null);

            e.Handled = true;
        }

        private void CommandBinding_WriteChangesCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Category is null)
                e.CanExecute = false;
            else
            {
                e.CanExecute = true;

                e.Handled = true;
            }
        }

        private void CommandBinding_WriteChangesCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(Category is not null);

            e.Handled = true;

            Category.WriteToDataBase();
        }

        #endregion

        private void CommandBinding_FetchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            Debug.Assert(Category is not null);

            e.Handled = true;
        }

        private void CommandBinding_FetchCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Debug.Assert(Category is not null);

            e.Handled = true;

            Category.UpdateDatabaseDataTable();
        }
    }
}
