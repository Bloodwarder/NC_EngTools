using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace LayersIO.ExternalData
{
    public class LayerPropertiesDictionary : ExternalRepository<string, LayerProps>
    {
        const string XmlPropsName = "Layer_Props.xml";

        private readonly Dictionary<string, LayerProps> defaultLayerProps = new Dictionary<string, LayerProps>();

        internal LayerPropertiesDictionary()
        {
            try
            {
                //var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LayerProps>>();
                //InstanceDictionary = service.CreateProvider(PathProvider.GetPath(XmlPropsName)).GetData();

                defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
                defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3, Red = 192, Green = 168, Blue = 110 });
                defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LinetypeScale = 0.8, LinetypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Properties);
            }
        }

        public bool TryGet(string key, [MaybeNullWhen(false)] out LayerProps value, bool enabledefaults)
        {
            bool success = TryGet(key, out value);
            if(!success && enabledefaults)
            {
                value = new LayerProps { ConstantWidth = 0.4, LinetypeScale = 1, LinetypeName = "Continuous", LineWeight = -3 };
                success = true;
            }
            return success;
        }

        public void Reload(ILayerDataWriter<string, LayerProps> primary, ILayerDataProvider<string, LayerProps> secondary)
        {
            ReloadInstance(primary, secondary);
        }
    }
}