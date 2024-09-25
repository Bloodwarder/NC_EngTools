using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoaderCore.Interfaces
{
    public interface IRepository<TKey, TValue>
    {
        public IEnumerable<TValue> GetAll();
        public IEnumerable<KeyValuePair<TKey, TValue>> GetKeyValuePairs();

        public TValue Get(TKey key);

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue? value);
        public bool Has(TKey key);

    }
}
