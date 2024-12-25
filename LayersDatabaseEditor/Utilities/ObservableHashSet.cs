using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;

namespace LayersDatabaseEditor.Utilities
{
    public class ObservableHashSet<T> : INotifyCollectionChanged, IEnumerable<T>, INotifyPropertyChanged//ObservableCollection<T>
    {
        private HashSet<T> _set = new HashSet<T>();

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        internal HashSet<T> ExposedSet => _set;

        public void Add(T item)
        {
            if (_set.Add(item))
            {
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item));
                //PropertyChanged?.Invoke(this, new(nameof(_set.Count)));
            }
        }

        public void Remove(T item)
        {
            int index = Array.IndexOf(_set.ToArray(), item);
            if (_set.Remove(item))
            {
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        public void Clear()
        {
            _set.Clear();
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset, null));
        }

        public bool Contains(T item) => _set.Contains(item);

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }
    }
}
