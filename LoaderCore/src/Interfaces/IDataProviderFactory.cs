using LoaderCore.Interfaces;

namespace LoaderCore.Interfaces

{
    public interface IDataProviderFactory<TKey, TValue> where TKey : notnull
    {
        public ILayerDataProvider<TKey, TValue> CreateProvider(string path);
    }
}