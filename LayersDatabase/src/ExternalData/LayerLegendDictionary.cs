using System.IO;
using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Utilities;

namespace LayersIO.ExternalData
{
    public class LayerLegendDictionary : ExternalDictionary<string, LegendData>
    {
        const string XmlLegendName = "Layer_Legend.xml";

        private static readonly LayerLegendDictionary instance;
        static LayerLegendDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDictionary();
        }
        LayerLegendDictionary()
        {
            try
            {
                InstanceDictionary = new XmlLayerDataProvider<string, LegendData>(PathProvider.GetPath(XmlLegendName)).GetData();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Legend);
            }
        }
        public static LegendData TryGetValue(string layername, out LegendData value)
        {
            return instance.TryGetInstanceValue(layername, out value);
        }
        public static void Reload(ILayerDataProvider<string, LegendData> primary, ILayerDataProvider<string, LegendData> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}