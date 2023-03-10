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
        private Category Category
        {
            get { Debug.Assert(_category is not null); return _category; }
            set { if (_category != value) _category = value; }
        }

        public UserControl_Category()
        {
            InitializeComponent();
        }

        #region Events

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is Category)
            {
                Category = (Category)DataContext;   // Take the default data object that the XAML constructed
                                                    // Be careful not to reconstruct _category, as we will lose access to the category object
                                                    // that the UC has been passed
            }
        }

        private void button_NewSymbolFieldMap_Click(object sender, RoutedEventArgs e)
        {
            Category.NewSymbolFieldMap();
        }

        #endregion
    }
}
