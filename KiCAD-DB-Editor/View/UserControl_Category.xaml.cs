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
    /// Interaction logic for UserControl_Category.xaml
    /// </summary>
    public partial class UserControl_Category : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty CategoryVMProperty = DependencyProperty.Register(
            nameof(CategoryVM),
            typeof(CategoryVM),
            typeof(UserControl_Category)
            );

        public CategoryVM CategoryVM
        {
            get => (CategoryVM)GetValue(CategoryVMProperty);
            set => SetValue(CategoryVMProperty, value);
        }

        public static readonly DependencyProperty ParameterVMsProperty = DependencyProperty.Register(
            nameof(ParameterVMs),
            typeof(ObservableCollectionEx<ParameterVM>),
            typeof(UserControl_Category)
            );

        public ObservableCollectionEx<ParameterVM> ParameterVMs
        {
            get => (ObservableCollectionEx<ParameterVM>)GetValue(ParameterVMsProperty);
            set => SetValue(ParameterVMsProperty, value);
        }

        #endregion

        public UserControl_Category()
        {
            InitializeComponent();
        }
    }
}
