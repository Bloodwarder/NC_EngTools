﻿using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.DataRepositories
{
    public class InMemoryRepository<TKey, TData> : IRepository<TKey, TData> where TKey : notnull, IEquatable<TKey>
    {
        private const string DatabaseFileName = "LayerData.db";

        private Dictionary<TKey, TData> _dictionary;
        private readonly ILayerDataProvider<TKey, TData> _dataProvider;

        public InMemoryRepository(IDataProviderFactory<TKey, TData> factory, IFilePathProvider pathProvider)
        {
            var path = pathProvider.GetPath(DatabaseFileName); // TODO : вставить универсальную конструкцию
            _dataProvider = factory.CreateProvider(path);
            _dictionary = _dataProvider.GetData();
        }

        public TData Get(TKey key)
        {
            bool success = _dictionary.TryGetValue(key, out var props);
            return success ? props! : throw new ArgumentException("Нет данных с указанным ключом");
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
            _dictionary = _dataProvider.GetData();
        }
    }

    
}
