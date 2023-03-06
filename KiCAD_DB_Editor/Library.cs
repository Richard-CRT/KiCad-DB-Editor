using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class Library : NotifyObject
    {
        private string? _dblFilePath;
        public string DblFilePath
        {
            get { Debug.Assert(_dblFilePath is not null); return _dblFilePath; }
            set
            {
                if (_dblFilePath != value)
                {
                    _dblFilePath = value;

                    InvokePropertyChanged();
                }
            }
        }

        public Library(string dblFilePath)
        {
            DblFilePath = dblFilePath;
        }
    }
}
