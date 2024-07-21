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
    /// Interaction logic for UserControl_Folder.xaml
    /// </summary>
    public partial class UserControl_Folder : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty FolderVMProperty = DependencyProperty.Register(
            nameof(FolderVM),
            typeof(FolderVM),
            typeof(UserControl_Folder)
            );

        public FolderVM FolderVM
        {
            get => (FolderVM)GetValue(FolderVMProperty);
            set => SetValue(FolderVMProperty, value);
        }

        #endregion

        public UserControl_Folder()
        {
            InitializeComponent();
        }
    }
}
