using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerLegendReader : IStandardReader<LegendData>
    {
        private static IDictionary<string, LegendData> _provider = NcetCore.ServiceProvider.GetService<IDictionary<string, LegendData>>();

        public LegendData GetStandard(string layerName)
        {
            _ = _provider.TryGetValue(layerName, out var legend);
            return legend ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LegendData? standard)
        {
            bool success = _provider.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
