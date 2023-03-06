using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class DataObj : NotifyObject
    {
        private Project? _project = null;
        public Project Project
        {
            get { Debug.Assert(_project is not null); return _project; }
            set
            {
                if (_project != value)
                {
                    _project = value;

                    InvokePropertyChanged();
                }
            }
        }

        public DataObj()
        {
            Project = new Project();
        }
    }
}
