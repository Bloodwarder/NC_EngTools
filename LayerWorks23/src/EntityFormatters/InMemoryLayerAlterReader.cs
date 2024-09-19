using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LoaderCore.Interfaces
{
    internal class InMemoryLayerAlterReader : IStandardReader<string>
    {
        private static LayerAlteringDictionary _provider = NcetCore.ServiceProvider.GetService<LayerAlteringDictionary>();

        public string GetStandard(string layerName)
        {
            _ = _provider.TryGetValue(layerName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard([MaybeNullWhen(false)]string layerName, out string? standard)
        {
            bool success = _provider.TryGetValue(layerName, out standard);
            return success;
        }
    }

}
