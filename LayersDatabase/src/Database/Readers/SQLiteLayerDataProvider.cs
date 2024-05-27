using LayersIO.ExternalData;

namespace LayersIO.Database.Readers
{
    internal abstract class SQLiteLayerDataProvider<TKey, TValue> : SQLiteLayerDataConnection, ILayerDataProvider<TKey, TValue> where TKey : notnull
    {
        internal SQLiteLayerDataProvider(string path) : base(path) { }
        public abstract Dictionary<TKey, TValue> GetData();
        public abstract TValue? GetItem(TKey key);
    }
}
