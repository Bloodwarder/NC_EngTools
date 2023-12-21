using System.Collections.Generic;
using System.IO;
using LayerWorks.LayerProcessing;

namespace LayerWorks.ExternalData
{
    internal class LayerPropertiesDictionary : ExternalDictionary<string, LayerProps>
    {
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
                InstanceDictionary = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props")).GetDictionary();

                defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
                defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 192, Green = 168, Blue = 110 });
                defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Properties);
            }
        }

        private protected LayerProps GetInstanceValue(string layername, out bool success, bool enabledefaults = true)
        {
            SimpleLayerParser slp;
            success = false;
            try
            {
                slp = new SimpleLayerParser(layername);
            }
            catch (WrongLayerException)
            {
                if (enabledefaults)
                {
                    success = true;
                    return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
            success = InstanceDictionary.TryGetValue(slp.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success = true;
                    return defaultLayerProps[slp.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        private protected LayerProps GetInstanceValue(LayerParser layerparser, out bool success, bool enabledefaults = true)
        {
            success = InstanceDictionary.TryGetValue(layerparser.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success = true;
                    return defaultLayerProps[layerparser.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        public static LayerProps GetValue(string layername, out bool success, bool enabledefaults = true)
        {
            return instance.GetInstanceValue(layername, out success, enabledefaults);
        }
        public static LayerProps GetValue(LayerParser layer, out bool success, bool enabledefaults = true)
        {
            return instance.GetInstanceValue(layer, out success, enabledefaults);
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