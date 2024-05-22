using KiCAD_DB_Editor.Model;
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

        #region Notify Properties

        public string Name
        {
            get { return Parameter.Name; }
            set { if (Parameter.Name != value) { Parameter.Name = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public ParameterVM(Model.Parameter parameter)
        {
            // Link model
            Parameter = parameter;
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
