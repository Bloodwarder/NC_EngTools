using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerLegendReader : IStandardReader<LegendData>
    {
        public LegendData GetStandard(string layerName)
        {
            _ = LayerLegendDictionary.TryGetValue(layerName, out var legend);
            return legend ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LegendData? standard)
        {
            bool success = LayerLegendDictionary.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
