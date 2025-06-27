using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Utilities
{
    public class ObservableDictionary<TKey, TValue> :
        Dictionary<TKey, TValue>, INotifyCollectionChanged
        where TKey : notnull
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                base[key] = value;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public new bool TryAdd(TKey key, TValue value)
        {
            bool val = base.TryAdd(key, value);
            if (val)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return val;
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            // Exception will throw if invalid
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public new bool Remove(TKey key)
        {
            bool val = base.Remove(key);
            if (val)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return val;
        }

        public new void Clear()
        {
            base.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
