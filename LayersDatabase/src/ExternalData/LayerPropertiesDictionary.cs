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
                InstanceDictionary = new XmlDictionaryDataProvider<string, LayerProps>(PathProvider.GetPath(XmlPropsName)).GetDictionary();

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

        private protected LayerProps GetInstanceValue(string layername, out bool success, bool enabledefaults = true)
        {
                    //return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };

            success = InstanceDictionary.TryGetValue(layername, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success = true;
                    return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };
                }
                else
                {
                    throw new System.Exception("Нет стандартов для слоя");
                }
            }
        }

        public static LayerProps GetValue(string layername, out bool success, bool enabledefaults = true)
        {
            return instance.GetInstanceValue(layername, out success, enabledefaults);
        }
        public static void Reload(DictionaryDataProvider<string, LayerProps> primary, DictionaryDataProvider<string, LayerProps> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }

        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}