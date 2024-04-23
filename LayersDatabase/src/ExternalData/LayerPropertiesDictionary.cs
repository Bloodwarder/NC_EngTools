using System.Collections.Generic;
using System.IO;
using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Utilities;

namespace LayersIO.ExternalData
{
    public class LayerPropertiesDictionary : ExternalDictionary<string, LayerProps>
    {
        const string XmlPropsName = "Layer_Props.xml";

        private static readonly LayerPropertiesDictionary instance;
        private readonly Dictionary<string, LayerProps> defaultLayerProps = new Dictionary<string, LayerProps>();
        static LayerPropertiesDictionary()
        {
            if (instance == null)
                instance = new LayerPropertiesDictionary();
        }

        internal LayerPropertiesDictionary()
        {
            try
            {
                InstanceDictionary = new XmlLayerDataProvider<string, LayerProps>(PathProvider.GetPath(XmlPropsName)).GetData();

                defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
                defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 192, Green = 168, Blue = 110 });
                defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Properties);
            }
        }

        private protected bool TryGetInstanceValue(string layername, out LayerProps value, bool enabledefaults = true)
        {
                    //return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };

            bool success = InstanceDictionary.TryGetValue(layername, out LayerProps layerProps);
            if (success)
            {
                value= layerProps;
                return success;
            }
            else
            {
                if (enabledefaults)
                {
                    value = new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };
                    return true;
                }
                else
                {
                    throw new System.Exception("Нет стандартов для слоя");
                }
            }
        }

        public static bool TryGetValue(string layername, out LayerProps value, bool enabledefaults = true)
        {
            return instance.TryGetInstanceValue(layername, out value, enabledefaults);
        }
        public static void Reload(ILayerDataProvider<string, LayerProps> primary, ILayerDataProvider<string, LayerProps> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }

        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}