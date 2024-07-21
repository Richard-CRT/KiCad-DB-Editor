using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class LibraryVM : NotifyObject
    {
        public readonly Model.Library Library;

        #region Notify Properties

        private ViewModel.FolderVM _topLevelFolderVM;
        public ViewModel.FolderVM TopLevelFolderVM
        {
            get { return _topLevelFolderVM; }
            set
            {
                if (_topLevelFolderVM != value)
                {
                    _topLevelFolderVM = value;
                    if (_topLevelFolderVMs is null)
                        TopLevelFolderVMs = new() { value };
                    else
                        TopLevelFolderVMs[0] = value;
                    InvokePropertyChanged();
                }
            }
        }

        private TreeViewItemVM _selectedTreeViewItemVM;
        public TreeViewItemVM SelectedTreeViewItemVM
        {
            get { return _selectedTreeViewItemVM; }
            set
            {
                if (_selectedTreeViewItemVM != value)
                {
                    _selectedTreeViewItemVM = value;
                    InvokePropertyChanged();
                }
            }
        }


        // Exists only to allow treeview to bind to a collection instead of single item, should not be used anywhere else
        private ObservableCollectionEx<ViewModel.FolderVM> _topLevelFolderVMs;
        public ObservableCollectionEx<ViewModel.FolderVM> TopLevelFolderVMs
        {
            get { return _topLevelFolderVMs; }
            private set { if (_topLevelFolderVMs != value) { _topLevelFolderVMs = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public LibraryVM(Model.Library library)
        {
            // Link model
            Library = library;

            TopLevelFolderVM = new(null, library.TopLevelFolder);
            Debug.Assert(_topLevelFolderVM is not null);
            Debug.Assert(_topLevelFolderVMs is not null);
        }

        public LibraryVM() : this(new()) { }


        #region Commands


        #endregion Commands
    }
}
