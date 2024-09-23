using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerPropsReader : IStandardReader<LayerProps>
    {
        private static IDictionary<string, LayerProps> _provider = NcetCore.ServiceProvider.GetService<IDictionary<string, LayerProps>>();
        public LayerProps GetStandard(string layerName)
        {
            _ = _provider.TryGetValue(layerName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LayerProps? standard)
        {
            bool success = _provider.TryGetValue(layerName, out standard);
            return success;
        }
    }
}
