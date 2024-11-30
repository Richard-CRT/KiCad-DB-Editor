using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.ViewModel;
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

            // UI Commands, only OK because it doesn't do anything with data
            // Setup commands
            EditCommand = new BasicCommand(EditCommandExecuted, EditCommandCanExecute);
        }

        private void editTextBox(TextBox tB)
        {
            tB.Visibility = Visibility.Visible;
            tB.Focus();
            tB.Select(0, tB.Text.Length);
        }

        private void contentControl_textBlockContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl cC && cC.Tag is TextBox tB)
            {
                editTextBox(tB);
                e.Handled = true;
            }
        }

        private void textBox_ParameterName_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tB)
            {
                tB.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                tB.Visibility = Visibility.Hidden;
            }
        }

        private void textBox_ParameterName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tB)
            {
                if (e.Key == Key.Escape)
                    tB.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

                if (e.Key == Key.Enter || e.Key == Key.Escape)
                {
                    var ancestor = tB.Parent;
                    while (ancestor is not null)
                    {
                        if (ancestor is UIElement uiE && uiE.Focusable)
                        {
                            uiE.Focus();
                            break;
                        }
                        ancestor = VisualTreeHelper.GetParent(ancestor);
                    }
                    e.Handled = true;
                }
            }
        }


        #region Commands

        public IBasicCommand EditCommand { get; }

        private bool EditCommandCanExecute(object? parameter)
        {
            return listBox_Parameters.SelectedItem is ParameterVM;
        }

        private void EditCommandExecuted(object? parameter)
        {
            Debug.Assert(listBox_Parameters.SelectedItem is ParameterVM);
            var pVM = (ParameterVM)listBox_Parameters.SelectedItem;
            var dO = listBox_Parameters.ItemContainerGenerator.ContainerFromItem(pVM);
            Debug.Assert(dO is UIElement);
            var uiE = (UIElement)dO;
            Debug.Assert(uiE is ListBoxItem);
            var lbI = (ListBoxItem)uiE;
            ContentPresenter? cp = findVisualChild<ContentPresenter>(lbI);
            Debug.Assert(cp is not null);
            DataTemplate dt = cp.ContentTemplate;
            var foundElement = dt.FindName("textBox_ParameterName", cp);
            Debug.Assert(foundElement is TextBox);
            TextBox tB = (TextBox)foundElement;
            editTextBox(tB);
        }

        #endregion Commands

        private childItem? findVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is not null && child is childItem)
                {
                    return (childItem)child;
                }
                else if (child is not null)
                {
                    childItem? childOfChild = findVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }

}
