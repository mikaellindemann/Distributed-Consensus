using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Client.ViewModels
{
    public class ObservableSet<T> : INotifyCollectionChanged, ICollection<T>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly ISet<T> _set;

        public ObservableSet()
        {
            _set = new HashSet<T>();
        }

        public void Clear()
        {
            _set.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        public void Add(T item)
        {
            _set.Add(item);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, 
                    new List<T> {item}));
        }

        public bool Remove(T item)
        {
            var result = _set.Remove(item);
            CollectionChanged?.Invoke(this, 
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, 
                    new List<T> {item}));
            return result;
        }

        public int Count => _set.Count;

        public bool IsReadOnly => _set.IsReadOnly;

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _set).GetEnumerator();
        }
    }
}
