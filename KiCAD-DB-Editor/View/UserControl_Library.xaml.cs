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

        bool _isDragging = false;

        private void treeViewItem_PreviewMouseDown(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (sender is TreeViewItem treeViewItem)
                {
                    treeViewItem.Focus();
                }
            }
            if (!_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
            }
        }

        private void treeView_SubLibraries_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragDrop.DoDragDrop(treeView_SubLibraries, treeView_SubLibraries.SelectedValue, DragDropEffects.Move);
                else
                    _isDragging = false;
            }
        }

        private bool dragValid(DragEventArgs e, out SubLibraryVM? sourceSubLibraryVM, out SubLibraryVM? targetSubLibraryVM)
        {
            sourceSubLibraryVM = null;
            targetSubLibraryVM = null;
            bool valid = false;
            if (e.Data.GetDataPresent(typeof(SubLibraryVM)))
            {
                sourceSubLibraryVM = (SubLibraryVM)e.Data.GetData(typeof(SubLibraryVM));
                if (sourceSubLibraryVM.ParentSubLibraryVM is not null)
                {
                    targetSubLibraryVM = _getItemAtLocation(e.GetPosition(treeView_SubLibraries));
                    if (targetSubLibraryVM is not null)
                    {
                        if (sourceSubLibraryVM != targetSubLibraryVM)
                        {
                            if (sourceSubLibraryVM.ParentSubLibraryVM != targetSubLibraryVM)
                            {
                                if (!sourceSubLibraryVM.RecursiveContains(targetSubLibraryVM))
                                    valid = true;
                            }
                        }
                    }
                }
            }
            return valid;
        }

        private void treeView_SubLibraries_DragOver(object sender, DragEventArgs e)
        {
            if (dragValid(e, out SubLibraryVM? sourceSubLibraryVM, out SubLibraryVM? targetSubLibraryVM))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void treeView_SubLibraries_Drop(object sender, DragEventArgs e)
        {
            // This shouldn't fail as long as we've done the check in DragOver properly
            if (dragValid(e, out SubLibraryVM? sourceSubLibraryVM, out SubLibraryVM? targetSubLibraryVM))
            {
                Debug.Assert(sourceSubLibraryVM is not null && targetSubLibraryVM is not null);
                // Code to move the item in the model is placed here...

                Debug.Assert(sourceSubLibraryVM.ParentSubLibraryVM is not null);

                if (sourceSubLibraryVM.ParentSubLibraryVM.RemoveSubLibraryCommand.CanExecute(sourceSubLibraryVM) &&
                    targetSubLibraryVM.AddSubLibraryCommand.CanExecute(sourceSubLibraryVM))
                {
                    sourceSubLibraryVM.ParentSubLibraryVM.RemoveSubLibraryCommand.Execute(sourceSubLibraryVM);
                    targetSubLibraryVM.AddSubLibraryCommand.Execute(sourceSubLibraryVM);
                }
            }
            e.Handled = true;
        }

        // This method credit to https://blogs.infosupport.com/implementing-simple-drag-and-drop-operations-on-a-wpf-treeview/
        private SubLibraryVM? _getItemAtLocation(Point location)
        {
            SubLibraryVM? foundItem = null;
            HitTestResult hitTestResults = VisualTreeHelper.HitTest(treeView_SubLibraries, location);

            if (hitTestResults.VisualHit is FrameworkElement fe)
            {
                object dataObject = fe.DataContext;

                if (dataObject is SubLibraryVM slVM)
                {
                    foundItem = slVM;
                }
            }

            return foundItem;
        }
    }
}
