using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerLegendDrawReader : IStandardReader<LegendDrawTemplate>
    {
        public LegendDrawTemplate GetStandard(string layerName)
        {
            _ = LayerLegendDrawDictionary.TryGetValue(layerName, out var drawTemplate);
            return drawTemplate ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LegendDrawTemplate? standard)
        {
            bool success = LayerLegendDrawDictionary.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
