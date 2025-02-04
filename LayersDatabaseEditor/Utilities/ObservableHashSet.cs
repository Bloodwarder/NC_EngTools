using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace LayersDatabaseEditor.Utilities
{
    //public class ObservableHashSet<T> : INotifyCollectionChanged, IEnumerable<T>, INotifyPropertyChanged//ObservableCollection<T>
    //{
    //    private HashSet<T> _set = new HashSet<T>();

    //    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    //    public event PropertyChangedEventHandler? PropertyChanged;

    //    internal HashSet<T> ExposedSet => _set;

    //    public void Add(T item)
    //    {
    //        if (_set.Add(item))
    //        {
    //            int index = Array.IndexOf(_set.ToArray(), item);
    //            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item, index));
    //            //PropertyChanged?.Invoke(this, new(nameof(_set.Count)));
    //        }
    //    }

    //    public void Remove(T item)
    //    {
    //        int index = Array.IndexOf(_set.ToArray(), item);
    //        if (_set.Remove(item))
    //        {
    //            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item, index));
    //        }
    //    }

    //    public void Clear()
    //    {
    //        _set.Clear();
    //        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset, null));
    //    }

    //    public bool Contains(T item) => _set.Contains(item);

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return _set.GetEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return _set.GetEnumerator();
    //    }
    //}

    public class ObservableHashSet<T> : ObservableCollection<T>
    {
         private HashSet<T> _hashSet = new();
        public ObservableHashSet() : base() { }

        public HashSet<T> ExposedSet => _hashSet;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _hashSet.Clear();
        }

        protected override void InsertItem(int index, T item)
        {
            if (!_hashSet.Contains(item))
            {
                base.InsertItem(index, item);
                _hashSet.Add(item);
            }
        }

        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            base.RemoveItem(index);
            _hashSet.Remove(removedItem);
        }

        protected override void SetItem(int index, T item)
        {
            T replacedItem = this[index];
            base.SetItem(index, item);
            _hashSet.Remove(replacedItem);
            _hashSet.Add(item);
        }
    }
}
