using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.EntityFormatters
{
    internal class InMemoryLayerLegendDrawRepository : IRepository<string, LegendDrawTemplate>
    {
        private static Dictionary<string, LegendDrawTemplate> _dictionary = null!;

        public InMemoryLayerLegendDrawRepository()
        {
            var factory = NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LegendDrawTemplate>>();
            var path = PathProvider.GetPath("LayerData_ИС.db"); // TODO : вставить универсальную конструкцию
            var reader = factory.CreateProvider(path);
            _dictionary = reader.GetData();
        }

        public LegendDrawTemplate Get(string key)
        {
            bool success = _dictionary.TryGetValue(key, out var props);
            return success ? props! : throw new NoPropertiesException("");
        }

        public IEnumerable<LegendDrawTemplate> GetAll() => _dictionary.Values;

        public IEnumerable<string> GetKeys() => _dictionary.Keys;

        public IEnumerable<KeyValuePair<string, LegendDrawTemplate>> GetKeyValuePairs() => _dictionary.AsEnumerable();

        public bool Has(string key) => _dictionary.ContainsKey(key);

        public bool TryGet(string key, [MaybeNullWhen(false)] out LegendDrawTemplate? value)
        {
            bool success = _dictionary.TryGetValue(key, out value);
            return success;
        }
    }

}
