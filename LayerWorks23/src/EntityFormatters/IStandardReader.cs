using NameClassifiers;
using LayersIO.DataTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using System.ComponentModel.Design;

namespace LayerWorks.EntityFormatters
{
    public interface IStandardReader<T>
    {
        public T GetStandard(string layerName);
        public T GetStandard(LayerInfo layerInfo);
        public bool TryGetStandard(string layerName, out T? standard);
        public bool TryGetStandard(LayerInfo layerInfo, out T? standard);

    }

    internal class InMemoryLayerPropsReader : IStandardReader<LayerProps>
    {
        public LayerProps GetStandard(string layerName)
        {
            _ = LayerPropertiesDictionary.TryGetValue(layerName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public LayerProps GetStandard(LayerInfo layerInfo)
        {
            _ = LayerPropertiesDictionary.TryGetValue(layerInfo.TrueName, out var props);
            return props ?? throw new NoPropertiesException("");
        }

        public bool TryGetStandard(string layerName, out LayerProps? standard)
        {
            bool success = LayerPropertiesDictionary.TryGetValue(layerName, out standard);
            return success;
        }

        public bool TryGetStandard(LayerInfo layerInfo, out LayerProps? standard)
        {
            bool success = LayerPropertiesDictionary.TryGetValue(layerInfo.TrueName, out standard);
            return success;
        }
    }
    
}
