using LoaderCore.Interfaces;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace LayersIO.ExternalData
{
    public abstract class ExternalDictionary<TKey, TValue> : IDictionary<TKey,TValue> where TKey : notnull
    {
        private protected Dictionary<TKey, TValue> InstanceDictionary { get; set; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)InstanceDictionary).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)InstanceDictionary).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).IsReadOnly;

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)InstanceDictionary)[key]; set => ((IDictionary<TKey, TValue>)InstanceDictionary)[key] = value; }

        private protected void ReloadInstance(ILayerDataWriter<TKey, TValue> primary, ILayerDataProvider<TKey, TValue> secondary)
        {
            InstanceDictionary = secondary.GetData();
            primary.OverwriteSource(InstanceDictionary);
        }

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)InstanceDictionary).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)InstanceDictionary).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)InstanceDictionary).Remove(key);
        }

        public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return ((IDictionary<TKey, TValue>)InstanceDictionary).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)InstanceDictionary).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)InstanceDictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)InstanceDictionary).GetEnumerator();
        }
    }
}