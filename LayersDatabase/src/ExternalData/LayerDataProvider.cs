using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public abstract class LayerDataProvider<TKey, TValue> where  TKey : class
    {

        public abstract Dictionary<TKey, TValue> GetData();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }
}