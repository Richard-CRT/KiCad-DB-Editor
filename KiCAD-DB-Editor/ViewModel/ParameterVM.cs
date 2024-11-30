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
    public class ParameterVM : NotifyObject
    {
        public readonly Model.Parameter Parameter;

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

        public ParameterVM(Model.Parameter parameter)
        {
            // Link model
            Parameter = parameter;
        }


        #region Commands


        #endregion Commands
    }
}
