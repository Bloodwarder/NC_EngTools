using System.Collections.Generic;

namespace LayersIO.ExternalData
{
    public interface ILayerDataProvider<TKey, TValue> where  TKey : class
    {

        public Dictionary<TKey, TValue> GetData();

    }
}