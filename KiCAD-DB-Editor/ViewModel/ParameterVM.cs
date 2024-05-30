using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class ParameterVM : NotifyObject, IComparable<ParameterVM>
    {
        public readonly Model.Parameter Parameter;
        public ViewModel.SubLibraryVM ParentSubLibraryVM;

        #region Notify Properties

        public string Name
        {
            get { return Parameter.Name; }
            set
            {
                if (Parameter.Name != value)
                {
                    try
                    {
                        if (EditCommand.CanExecute(value))
                            EditCommand.Execute(value);
                    }
                    catch (ArgumentValidationException ex)
                    {
                        // Breaks MVVM but not worth the effort to respect MVVM for this
                        (new Window_ErrorDialog(ex.Message)).ShowDialog();
                    }
                }
            }
        }

        #endregion Notify Properties

        public ParameterVM(ViewModel.SubLibraryVM parentSubLibraryVM, Model.Parameter parameter)
        {
            // Link model
            Parameter = parameter;

            ParentSubLibraryVM = parentSubLibraryVM;

            // Setup commands
            EditCommand = new BasicCommand(EditCommandExecuted, EditCommandCanExecute);
        }


        #region Commands

        public IBasicCommand EditCommand { get; }

        private bool EditCommandCanExecute(object? parameter)
        {
            return parameter is string newName;
        }

        private void EditCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is string);
            string newName = (string)parameter;

            if (newName.Length < 1 || newName.Length > 100)
                throw new ArgumentValidationException("New name is invalid length");

            if (ParentSubLibraryVM.ParameterVMs.Where(pVM => pVM.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)).Any())
                throw new ArgumentValidationException("Parameter name already exists");

            var conflictingParentParameterVMs = ParentSubLibraryVM.InheritedParameterVMs.Where(pVM => pVM.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (conflictingParentParameterVMs.Any())
                throw new ArgumentValidationException($"Parameter name already exists in parent folder:\r\n{string.Join("\r\n", conflictingParentParameterVMs.Select(pVM => pVM.ParentSubLibraryVM.Path))}");

            var conflictingRecursiveParameterVMs = ParentSubLibraryVM.RecursiveParameterVMs().Where(pVM => pVM.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (conflictingRecursiveParameterVMs.Any())
                throw new ArgumentValidationException($"Parameter name already exists in sub-folder(s):\r\n{string.Join("\r\n", conflictingRecursiveParameterVMs.Select(pVM => pVM.ParentSubLibraryVM.Path))}");

            Parameter.Name = newName;
            InvokePropertyChanged(nameof(ParameterVM.Name));
        }

        #endregion Commands

        public int CompareTo(ViewModel.ParameterVM? other)
        {
            if (other is null)
                return 1;
            else
                return this.Name.CompareTo(other.Name);
        }
    }
}
