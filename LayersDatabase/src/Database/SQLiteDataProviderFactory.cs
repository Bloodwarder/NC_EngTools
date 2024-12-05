using LoaderCore.Interfaces;

namespace LayersIO.Database
{
    public class SQLiteDataProviderFactory<TKey, TValue> : IDataProviderFactory<TKey, TValue> where TKey : notnull
    {
        private readonly Func<string, ILayerDataProvider<TKey, TValue>> _providerFactioryMethod;
        public SQLiteDataProviderFactory(Func<string, ILayerDataProvider<TKey, TValue>> providerFactioryMethod)
        {
            _providerFactioryMethod = providerFactioryMethod;
        }
        public ILayerDataProvider<TKey, TValue> CreateProvider(string path)
        {
            return _providerFactioryMethod(path);
        }
    }
}
