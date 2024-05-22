using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using KiCAD_DB_Editor;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.Commands;
using System.Diagnostics;
using System.Security.Cryptography;

namespace KiCAD_DB_Editor.ViewModel
{
    public class MainWindowVM : NotifyObject
    {
        #region Notify Properties

        private string _windowTitle = $"KiCAD DB Editor";
        public string WindowTitle
        {
            get { return _windowTitle; }
            set { if (_windowTitle != value) { _windowTitle = value; InvokePropertyChanged(); } }
        }

        private ViewModel.LibraryVM? _libraryVM;
        public ViewModel.LibraryVM? LibraryVM
        {
            get { return _libraryVM; }
            set { if (_libraryVM != value) { _libraryVM = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public MainWindowVM()
        {
            // Setup commands
            SaveLibraryCommand = new BasicCommand(SaveLibraryCommandExecuted, SaveLibraryCommandCanExecute);
        }

        public void Loaded()
        {
            /*
            Model.Library lib = new();
            lib.TopLevelSubLibrary.SubLibraries.Add(new("1"));
            lib.TopLevelSubLibrary.SubLibraries.Add(new("2"));
            lib.TopLevelSubLibrary.SubLibraries[1].SubLibraries.Add(new("3"));
            lib.TopLevelSubLibrary.SubLibraries[1].SubLibraries.Add(new("4"));
            lib.TopLevelSubLibrary.Parameters.Add(new("param1"));

            LibraryVM = new(lib);

            lib.WriteToFile("test.tmp");
            */
            LibraryVM = new(Library.FromFile("test.tmp"));
            //LibraryVM.Library.WriteToFile("test.tmp");
        }


        #region Commands

        public IBasicCommand SaveLibraryCommand { get; }

        private bool SaveLibraryCommandCanExecute(object? parameter)
        {
            return LibraryVM is not null;
        }

        private void SaveLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(LibraryVM is not null);
            LibraryVM.Library.WriteToFile("test.tmp");
        }

        #endregion Commands
    }
}
