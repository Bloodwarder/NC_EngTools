using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerAlterReader : IStandardReader<string>
    {
        public string GetStandard(string layerName)
        {
            _ = LayerAlteringDictionary.TryGetValue(layerName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out string? standard)
        {
            bool success = LayerAlteringDictionary.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
