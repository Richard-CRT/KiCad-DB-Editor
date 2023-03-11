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
    /// Interaction logic for UserControl_SymbolFieldMap.xaml
    /// </summary>
    public partial class UserControl_SymbolFieldMap : UserControl
    {
        private SymbolFieldMap? _symbolFieldMap = null;
        private SymbolFieldMap? SymbolFieldMap
        {
            get { return _symbolFieldMap; }
            set { if (_symbolFieldMap != value) _symbolFieldMap = value; }
        }

        public UserControl_SymbolFieldMap()
        {
            InitializeComponent();
        }

        #region Events

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is SymbolFieldMap dC)
            {
                SymbolFieldMap = dC;   // Take the default data object that the XAML constructed
                                                                // Be careful not to reconstruct _symbolFieldMap, as we will lose access to the symbol field map object
                                                                // that the UC has been passed
            }
            else
                SymbolFieldMap = null;
        }

        #endregion
    }
}
