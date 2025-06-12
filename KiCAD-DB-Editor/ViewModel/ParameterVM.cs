using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using KiCAD_DB_Editor.ViewModel.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class ParameterVM : NotifyObject
    {
        public readonly Model.Parameter Parameter;
        public readonly LibraryVM ParentLibraryVM;

        #region Notify Properties

        public string Name
        {
            get { return Parameter.Name; }
            set
            {
                if (Parameter.Name != value)
                {
                    string lowerValue = value.ToLower();
                    if (value.Length == 0 || lowerValue.Any(c => !Util.SafeParameterCharacters.Contains(c)))
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    if (Util.ReservedParameterNames.Contains(lowerValue) || Util.ReservedParameterNameStarts.Any(s => lowerValue.StartsWith(s)))
                        throw new Exceptions.ArgumentValidationException("Proposed name not permitted");

                    if (ParentLibraryVM.ParameterVMs.Any(cVM => cVM.Name.ToLower() == lowerValue))
                        throw new Exceptions.ArgumentValidationException("Parent already contains parameter with proposed name");

                    Parameter.Name = value;
                    InvokePropertyChanged();
                }
            }
        }

        public string UUID
        {
            get { return Parameter.UUID; }
        }

        #endregion Notify Properties

        public ParameterVM(LibraryVM parentLibraryVM, Model.Parameter parameter)
        {
            ParentLibraryVM = parentLibraryVM;

            // Link model
            Parameter = parameter;
        }


        #region Commands


        #endregion Commands
    }
}
