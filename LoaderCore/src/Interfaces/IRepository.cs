using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LoaderCore.Interfaces
{
    public interface IRepository<TKey, TValue>
    {
        public IEnumerable<TValue> GetAll();
        public IEnumerable<KeyValuePair<TKey, TValue>> GetKeyValuePairs();

        public TValue Get(TKey key);

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue? value);
        public bool Has(TKey key);
        public IEnumerable<TKey> GetKeys();
    }
}
