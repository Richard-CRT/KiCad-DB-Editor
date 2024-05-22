using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        private bool _notificationSupressed = false;
        private bool _supressNotification = false;
        public bool SupressNotification
        {
            get
            {
                return _supressNotification;
            }
            set
            {
                _supressNotification = value;
                if (_supressNotification == false && _notificationSupressed)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _notificationSupressed = false;
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SupressNotification)
            {
                _notificationSupressed = true;
                return;
            }
            base.OnCollectionChanged(e);
        }

        public ObservableCollectionEx() : base() { }
        public ObservableCollectionEx(IEnumerable<T> collection) : base(collection) { }
        public ObservableCollectionEx(IList<T> collection) : base(collection) { }
    }
}
