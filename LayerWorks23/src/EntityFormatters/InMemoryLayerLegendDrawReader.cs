using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerLegendDrawReader : IStandardReader<LegendDrawTemplate>
    {
        private static IDictionary<string, LegendDrawTemplate> _provider = LoaderExtension.ServiceProvider.GetService<IDictionary<string, LegendDrawTemplate>>();

        public LegendDrawTemplate GetStandard(string layerName)
        {
            _ = _provider.TryGetValue(layerName, out var drawTemplate);
            return drawTemplate ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LegendDrawTemplate? standard)
        {
            bool success = _provider.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
