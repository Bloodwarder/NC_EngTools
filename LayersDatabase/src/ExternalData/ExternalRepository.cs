using LoaderCore;
using LoaderCore.Interfaces;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace LayersIO.ExternalData
{
    public abstract class ExternalRepository<TKey, TValue> : IRepository<TKey, TValue> where TKey : notnull, IEquatable<TKey>
    {

        ILayerDataWriter<TKey, TValue> _writer;
        ILayerDataProvider<TKey, TValue> _reader;

        public ExternalRepository()
        {
            //_writer = NcetCore.ServiceProvider.GetService<ILayerDataWriter<TKey, TValue>>();
            //_reader = NcetCore.ServiceProvider.GetService<ILayerDataProvider<TKey, TValue>>();
        }

        private protected void ReloadInstance(ILayerDataWriter<TKey, TValue> primary, ILayerDataProvider<TKey, TValue> secondary)
        {
            var dictionary = secondary.GetData();
            primary.OverwriteSource(dictionary);
        }



        public IEnumerable<TValue> GetAll()
        {
            throw new NotImplementedException();
        }

        public TValue Get(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue? value)
        {
            throw new NotImplementedException();
        }

        public bool Has(TKey key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetKeyValuePairs()
        {
            throw new NotImplementedException();
        }
    }
}