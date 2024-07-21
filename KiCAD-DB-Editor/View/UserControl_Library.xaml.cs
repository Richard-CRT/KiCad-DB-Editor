using KiCAD_DB_Editor.View.Dialogs;
using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for UserControl_Library.xaml
    /// </summary>
    public partial class UserControl_Library : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty LibraryVMProperty = DependencyProperty.Register(
            nameof(LibraryVM),
            typeof(LibraryVM),
            typeof(UserControl_Library)
            );

        public LibraryVM LibraryVM
        {
            get => (LibraryVM)GetValue(LibraryVMProperty);
            set => SetValue(LibraryVMProperty, value);
        }

        #endregion

        public UserControl_Library()
        {
            InitializeComponent();
        }
    }
}
