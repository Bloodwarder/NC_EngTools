using LoaderCore.Interfaces;

namespace LayersIO.Database
{
    public class SQLiteDataProviderFactory<TKey, TValue> : IDataProviderFactory<TKey, TValue> where TKey : notnull
    {
        private readonly Func<string, ILayerDataProvider<TKey, TValue>> _providerFactoryMethod;
        public SQLiteDataProviderFactory(Func<string, ILayerDataProvider<TKey, TValue>> providerFactoryMethod)
        {
            _providerFactoryMethod = providerFactoryMethod;
        }
        public ILayerDataProvider<TKey, TValue> CreateProvider(string path)
        {
            return _providerFactoryMethod(path);
        }
    }
}
