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
                        if (value.Length < 1 || value.Length > 100)
                            throw new ArgumentValidationException("Name length is invalid");

                        if (ParentSubLibraryVM.ParameterVMs.Select(pVM => pVM.Name).Contains(value))
                            throw new ArgumentValidationException("Parameter name already exists");

                        Parameter.Name = value;
                        InvokePropertyChanged();
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
        }

        public int CompareTo(ViewModel.ParameterVM? other)
        {
            if (other is null)
                return 1;
            else
                return this.Name.CompareTo(other.Name);
        }
    }
}
