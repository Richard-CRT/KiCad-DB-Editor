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

            // Setup commands
            EditSubLibraryCommand = new BasicCommand(EditSubLibraryCommandExecuted, EditSubLibraryCommandCanExecute);
            NewSubLibraryCommand = new BasicCommand(NewSubLibraryCommandExecuted, NewSubLibraryCommandCanExecute);
            DeleteSubLibraryCommand = new BasicCommand(DeleteSubLibraryCommandExecuted, DeleteSubLibraryCommandCanExecute);
        }

        public LibraryVM() : this(new()) { }


        #region Commands

        public IBasicCommand EditSubLibraryCommand { get; }
        public IBasicCommand NewSubLibraryCommand { get; }
        public IBasicCommand DeleteSubLibraryCommand { get; }

        private bool EditSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM slVM && slVM.ParentSubLibraryVM is not null;
        }

        private void EditSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);
            var slVM = (SubLibraryVM)parameter;
            Debug.Assert(slVM.ParentSubLibraryVM is not null);

            // Breaks MVVM but not worth the effort to respect MVVM for this
            var dialog = new Window_EditSubLibrary(slVM.Name);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var pair = (slVM, dialog.SubLibraryName);
                    if (slVM.ParentSubLibraryVM.EditSubLibraryCommand.CanExecute(pair))
                        slVM.ParentSubLibraryVM.EditSubLibraryCommand.Execute(pair);
                    else
                        // Breaks MVVM but not worth the effort to respect MVVM for this
                        (new Window_ErrorDialog("Name is invalid")).ShowDialog();
                }
                catch (ArgumentValidationException)
                {
                    // Breaks MVVM but not worth the effort to respect MVVM for this
                    (new Window_ErrorDialog("Name is invalid")).ShowDialog();
                }
            }
        }

        private bool NewSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM slVM;
        }

        private void NewSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);
            var slVM = (SubLibraryVM)parameter;
            // Breaks MVVM but not worth the effort to respect MVVM for this
            var dialog = new Window_EditSubLibrary("");
            if (dialog.ShowDialog() == true)
            {
                SubLibraryVM newSLVM = new();
                try
                {
                    newSLVM.Name = dialog.SubLibraryName;
                    if (slVM.AddSubLibraryCommand.CanExecute(newSLVM))
                        slVM.AddSubLibraryCommand.Execute(newSLVM);
                    else
                        // Breaks MVVM but not worth the effort to respect MVVM for this
                        (new Window_ErrorDialog("Unable to add sub-folder")).ShowDialog();
                }
                catch (ArgumentValidationException)
                {
                    // Breaks MVVM but not worth the effort to respect MVVM for this
                    (new Window_ErrorDialog("Name is invalid")).ShowDialog();
                }
            }
        }

        private bool DeleteSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM slVM && slVM.ParentSubLibraryVM is not null && slVM.ParentSubLibraryVM.RemoveSubLibraryCommand.CanExecute(slVM);
        }

        private void DeleteSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);
            var slVM = (SubLibraryVM)parameter;
            Debug.Assert(slVM.ParentSubLibraryVM is not null);

            if ((new Window_AreYouSureDialog()).ShowDialog() == true)
            {
                if (slVM.ParentSubLibraryVM.RemoveSubLibraryCommand.CanExecute(slVM))
                    slVM.ParentSubLibraryVM.RemoveSubLibraryCommand.Execute(slVM);
                else
                    // Breaks MVVM but not worth the effort to respect MVVM for this
                    (new Window_ErrorDialog("Unable to remove sub-folder")).ShowDialog();
            }
        }

        #endregion Commands
    }
}
