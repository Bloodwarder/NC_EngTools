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
        public static LegendData GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(LayerDataProvider<string, LegendData> primary, LayerDataProvider<string, LegendData> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}