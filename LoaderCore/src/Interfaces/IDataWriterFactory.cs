using LoaderCore.Interfaces;

namespace LoaderCore.Interfaces
{
    public interface IDataWriterFactory<TKey, TValue> where TKey : notnull
    {
        public ILayerDataWriter<TKey, TValue> CreateWriter(string path);
    }
}