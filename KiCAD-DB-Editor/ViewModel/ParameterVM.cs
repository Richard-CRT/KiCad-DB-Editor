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
        public ViewModel.FolderVM ParentFolderVM;

        #region Notify Properties

        public string Name
        {
            get { return Parameter.Name; }
            set
            {
                if (Parameter.Name != value)
                {
                    Parameter.Name = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public ParameterVM(ViewModel.FolderVM parentFolderVM, Model.Parameter parameter)
        {
            // Link model
            Parameter = parameter;

            ParentFolderVM = parentFolderVM;
        }


        #region Commands


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
