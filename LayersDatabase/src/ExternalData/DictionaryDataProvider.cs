using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public abstract class DictionaryDataProvider<TKey, TValue> where TKey : notnull
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }
}