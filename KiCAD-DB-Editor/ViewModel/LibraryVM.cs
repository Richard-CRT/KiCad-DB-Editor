using KiCAD_DB_Editor.Model;
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

        private ViewModel.SubLibraryVM _topLevelSubLibraryVM;
        public ViewModel.SubLibraryVM TopLevelSubLibraryVM
        {
            get { return _topLevelSubLibraryVM; }
            set
            {
                if (_topLevelSubLibraryVM != value)
                {
                    _topLevelSubLibraryVM = value;
                    if (_topLevelSubLibraryVMs is null)
                        TopLevelSubLibraryVMs = new() { value };
                    else
                        TopLevelSubLibraryVMs[0] = value;
                    InvokePropertyChanged();
                }
            }
        }

        // Exists only to allow treeview to bind to a collection instead of single item, should not be used anywhere else
        private ObservableCollectionEx<ViewModel.SubLibraryVM> _topLevelSubLibraryVMs;
        public ObservableCollectionEx<ViewModel.SubLibraryVM> TopLevelSubLibraryVMs
        {
            get { return _topLevelSubLibraryVMs; }
            private set { if (_topLevelSubLibraryVMs != value) { _topLevelSubLibraryVMs = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public LibraryVM(Model.Library library)
        {
            // Link model
            Library = library;

            TopLevelSubLibraryVM = new(null, library.TopLevelSubLibrary);
            Debug.Assert(_topLevelSubLibraryVM is not null);
            Debug.Assert(_topLevelSubLibraryVMs is not null);
        }
    }
}
