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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KiCAD_DB_Editor
{
    /// <summary>
    /// Interaction logic for UserControl_SubLibrary.xaml
    /// </summary>
    public partial class UserControl_SubLibrary : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty SubLibraryVMProperty = DependencyProperty.Register(
            nameof(SubLibraryVM),
            typeof(SubLibraryVM),
            typeof(UserControl_SubLibrary)
            );

        public SubLibraryVM SubLibraryVM
        {
            get => (SubLibraryVM)GetValue(SubLibraryVMProperty);
            set => SetValue(SubLibraryVMProperty, value);
        }

        #endregion

        public UserControl_SubLibrary()
        {
            InitializeComponent();
        }
    }
}
