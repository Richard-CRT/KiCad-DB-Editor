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

namespace KiCAD_DB_Editor.View
{
    /// <summary>
    /// Interaction logic for UserControl_Parameters.xaml
    /// </summary>
    public partial class UserControl_Parameters : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ParameterVMsProperty = DependencyProperty.Register(
            nameof(ParameterVMs),
            typeof(ObservableCollectionEx<ParameterVM>),
            typeof(UserControl_Parameters)
            );

        public ObservableCollectionEx<ParameterVM> ParameterVMs
        {
            get => (ObservableCollectionEx<ParameterVM>)GetValue(ParameterVMsProperty);
            set => SetValue(ParameterVMsProperty, value);
        }

        public static readonly DependencyProperty InheritedParameterVMsProperty = DependencyProperty.Register(
            nameof(InheritedParameterVMs),
            typeof(IEnumerable<ParameterVM>),
            typeof(UserControl_Parameters)
            );

        public IEnumerable<ParameterVM> InheritedParameterVMs
        {
            get => (IEnumerable<ParameterVM>)GetValue(InheritedParameterVMsProperty);
            set => SetValue(InheritedParameterVMsProperty, value);
        }

        #endregion

        public UserControl_Parameters()
        {
            InitializeComponent();
        }

        private void contentControl_textBlockContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl cC && cC.Tag is TextBox tB)
            {
                tB.Visibility = Visibility.Visible;
                tB.Focus();
                tB.Select(0, tB.Text.Length);
                e.Handled = true;
            }
        }

        private void textBox_ParameterName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tB)
            {
                tB.Visibility = Visibility.Hidden;
            }
        }
    }
}
