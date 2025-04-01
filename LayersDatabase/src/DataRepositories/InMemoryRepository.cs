using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.DataRepositories
{
    public class InMemoryRepository<TKey, TData> : IRepository<TKey, TData> where TKey : notnull, IEquatable<TKey>
    {
        private Dictionary<TKey, TData> _dictionary;
        private readonly ILogger? _logger;
        //private readonly ILayerDataProvider<TKey, TData> _dataProvider;

        public InMemoryRepository(ILayerDataProvider<TKey, TData> provider, ILogger? logger)
        {
            //_dataProvider = provider;
            _logger = logger;
            _dictionary = provider.GetData();
        }

        public TData Get(TKey key)
        {
            bool success = _dictionary.TryGetValue(key, out var props);
            return success ? props! : throw new ArgumentException($"Нет данных с указанным ключом (\"{key}\")");
        }

        public IEnumerable<TData> GetAll()
        {
            return _dictionary.Values;
        }

        public IEnumerable<TKey> GetKeys()
        {
            return _dictionary.Keys;
        }

        public IEnumerable<KeyValuePair<TKey, TData>> GetKeyValuePairs()
        {
            return _dictionary.AsEnumerable();
        }

        public bool Has(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TData? value)
        {
            bool success = _dictionary.TryGetValue(key, out value);
            return success;
        }

        public void Reload()
        {
            var provider = NcetCore.ServiceProvider.GetService<ILayerDataProvider<TKey, TData>>();
            if (provider != null) 
            {
                _dictionary = provider.GetData();
            }
            else
            {
                _logger?.LogWarning("Не удалось соединиться с источником данных для перезагрузки");
            }
            
        }
    }

    
}
