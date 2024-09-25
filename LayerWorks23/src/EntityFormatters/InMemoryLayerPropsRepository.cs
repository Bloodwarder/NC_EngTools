using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LayerWorks.EntityFormatters
{
    internal class InMemoryLayerPropsRepository : IRepository<string, LayerProps>
    {
        private static Dictionary<string, LayerProps> _dictionary = null!;

        public InMemoryLayerPropsRepository()
        {
            var factory = NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LayerProps>>();
            var path = PathProvider.GetPath("LayerData_ИС.db"); // TODO : вставить универсальную конструкцию
            var reader = factory.CreateProvider(path);
            _dictionary = reader.GetData();
        }

        public LayerProps Get(string key)
        {
            bool success = _dictionary.TryGetValue(key, out var props);
            return success ? props! : throw new NoPropertiesException("");
        }

        public IEnumerable<LayerProps> GetAll()
        {
            return _dictionary.Values;
        }

        public IEnumerable<KeyValuePair<string, LayerProps>> GetKeyValuePairs()
        {
            return _dictionary.AsEnumerable();
        }

        public bool Has(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGet(string key, [MaybeNullWhen(false)] out LayerProps? value)
        {
            bool success = _dictionary.TryGetValue(key, out value);
            return success;
        }
    }

    
}
