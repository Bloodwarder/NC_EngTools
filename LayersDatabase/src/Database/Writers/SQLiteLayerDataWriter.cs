using LayersIO.ExternalData;

namespace LayersIO.Database.Writers
{
    internal abstract class SQLiteLayerDataWriter<TKey, TValue> : SQLiteLayerDataConnection, ILayerDataWriter<TKey, TValue> where TKey : notnull
    {
        internal SQLiteLayerDataWriter(string path) : base(path) { }

        public abstract void OverwriteItem(Dictionary<TKey, TValue> dictionary);
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);
    }
}
