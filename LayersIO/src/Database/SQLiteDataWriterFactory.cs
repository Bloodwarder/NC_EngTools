using LoaderCore.Interfaces;

namespace LayersIO.Database
{
    public class SQLiteDataWriterFactory<TKey, TValue> : IDataWriterFactory<TKey,TValue> where TKey : notnull
    {
        private readonly Func<string, ILayerDataWriter<TKey, TValue>> _writerFactoryMethod;

        public SQLiteDataWriterFactory(Func<string, ILayerDataWriter<TKey, TValue>> writerFactioryMethod)
        {
            _writerFactoryMethod = writerFactioryMethod;
        }

        public ILayerDataWriter<TKey, TValue> CreateWriter(string path)
        {
            return _writerFactoryMethod(path);
        }
    }
}
