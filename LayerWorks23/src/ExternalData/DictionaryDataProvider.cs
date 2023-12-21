using System.Collections.Generic;

namespace LayerWorks.ExternalData
{
    internal abstract class DictionaryDataProvider<TKey, TValue>
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }
}