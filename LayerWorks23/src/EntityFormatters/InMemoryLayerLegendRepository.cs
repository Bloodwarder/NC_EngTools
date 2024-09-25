using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.EntityFormatters
{
    internal class InMemoryLayerLegendRepository : IRepository<string, LegendData>
    {
        private static Dictionary<string, LegendData> _dictionary = null!;

        public InMemoryLayerLegendRepository()
        {
            var factory = NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LegendData>>();
            var path = PathProvider.GetPath("LayerData_ИС.db"); // TODO : вставить универсальную конструкцию
            var reader = factory.CreateProvider(path);
            _dictionary = reader.GetData();
        }

        public LegendData Get(string key)
        {
            bool success = _dictionary.TryGetValue(key, out var value);
            return success ? value! : throw new NoPropertiesException("");
        }

        public IEnumerable<LegendData> GetAll() => _dictionary.Values;

        public IEnumerable<string> GetKeys() => _dictionary.Keys;

        public IEnumerable<KeyValuePair<string, LegendData>> GetKeyValuePairs() => _dictionary.AsEnumerable();

        public bool Has(string key) => _dictionary.ContainsKey(key);

        public bool TryGet(string key, [MaybeNullWhen(false)] out LegendData? value)
        {
            bool success = _dictionary.TryGetValue(key, out value);
            return success;
        }
    }

}
