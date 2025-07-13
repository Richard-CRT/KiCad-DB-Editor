using KiCad_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Utilities
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
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _notificationSupressed = false;
                }
            }
        }

        public void AddRange(IEnumerable<T> range)
        {
            var backUp = SupressNotification;
            SupressNotification = true;
            foreach (var item in range)
                Add(item);
            SupressNotification = backUp;
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
