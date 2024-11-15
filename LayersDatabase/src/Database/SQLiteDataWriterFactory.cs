using LoaderCore.Interfaces;

namespace LayersIO.Database
{
    public class SQLiteDataWriterFactory<TKey, TValue> where TKey : notnull
    {
        private readonly Func<string, ILayerDataWriter<TKey, TValue>> _writerFactioryMethod;

        public SQLiteDataWriterFactory(Func<string, ILayerDataWriter<TKey, TValue>> writerFactioryMethod)
        {
            _writerFactioryMethod = writerFactioryMethod;
        }

        public ILayerDataWriter<TKey, TValue> CreateWriter(string path)
        {
            return _writerFactioryMethod(path);
        }
    }
}
