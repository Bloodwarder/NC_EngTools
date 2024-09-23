using System.Collections.Generic;

namespace LoaderCore.Interfaces
{
    public interface ILayerDataProvider<TKey, TValue> where TKey : notnull
    {

        public Dictionary<TKey, TValue> GetData();

        public TValue? GetItem(TKey key);

    }
}