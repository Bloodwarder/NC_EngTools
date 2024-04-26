using System.IO;
using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Utilities;

namespace LayersIO.ExternalData
{
    public class LayerLegendDrawDictionary : ExternalDictionary<string, LegendDrawTemplate>
    {
        const string XmlLegendDrawName = "Layer_LegendDraw.xml";
        private static readonly LayerLegendDrawDictionary instance;
        static LayerLegendDrawDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDrawDictionary();
        }
        LayerLegendDrawDictionary()
        {
            try
            {
                InstanceDictionary = new XmlLayerDataProvider<string, LegendDrawTemplate>(PathProvider.GetPath(XmlLegendDrawName)).GetData();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.LegendDraw);
            }
        }
        public static bool TryGetValue(string layername, out LegendDrawTemplate? value)
        {
            return instance.TryGetInstanceValue(layername, out value);
        }
        public static void Reload(ILayerDataWriter<string, LegendDrawTemplate> primary, ILayerDataProvider<string, LegendDrawTemplate> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}