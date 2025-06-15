using KiCAD_DB_Editor.Model.Json;
using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Parameter : NotifyObject
    {
        #region Notify Properties

        private string _uuid = "";
        public string UUID
        {
            get { return _uuid; }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { if (_name != value) _name = value; InvokePropertyChanged(); }
        }

        #endregion Notify Properties

        public Parameter(JsonParameter jsonParameter)
        {
            this._uuid = jsonParameter.UUID;
            this.Name = jsonParameter.Name;
        }

        public Parameter(string name)
        {
            this._uuid = Guid.NewGuid().ToString();
            this.Name = name;
        }
    }
}
