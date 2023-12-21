using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public abstract class DictionaryDataProvider<TKey, TValue>
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }
}