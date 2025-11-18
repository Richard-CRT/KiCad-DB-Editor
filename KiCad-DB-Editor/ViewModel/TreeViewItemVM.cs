using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.ViewModel
{
    public class TreeViewItemVM : NotifyObject
    {
        #region Notify Properties

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion
    }
}
