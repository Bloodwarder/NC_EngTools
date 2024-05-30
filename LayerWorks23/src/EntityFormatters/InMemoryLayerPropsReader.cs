using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerPropsReader : IStandardReader<LayerProps>
    {
        public LayerProps GetStandard(string layerName)
        {
            _ = LayerPropertiesDictionary.TryGetValue(layerName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LayerProps? standard)
        {
            bool success = LayerPropertiesDictionary.TryGetValue(layerName, out standard);
            return success;
        }
    }
}
