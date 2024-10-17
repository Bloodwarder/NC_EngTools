﻿using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.DataRepositories
{
    internal class InMemoryLayerAlterRepository : IRepository<string, string>
    {
        private static Dictionary<string, string> _dictionary = null!;

        public InMemoryLayerAlterRepository()
        {
            var factory = NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, string>>();
            var path = PathProvider.GetPath("LayerData_ИС.db"); // TODO : вставить универсальную конструкцию
            var reader = factory.CreateProvider(path);
            _dictionary = reader.GetData();
        }

        public string Get(string key)
        {
            bool success = _dictionary.TryGetValue(key, out var value);
            return success ? value ! : throw new NoPropertiesException("");
        }

        public IEnumerable<string> GetAll() => _dictionary.Values;

        public IEnumerable<string> GetKeys() => _dictionary.Keys;

        public IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs() => _dictionary.AsEnumerable();

        public bool Has(string key) => _dictionary.ContainsKey(key);

        public bool TryGet(string key, [MaybeNullWhen(false)] out string? value)
        {
            bool success = _dictionary.TryGetValue(key, out value);
            return success;
        }
    }

}
