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
                InstanceDictionary = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathProvider.GetPath(XmlLegendDrawName)).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.LegendDraw);
            }
        }
        public static LegendDrawTemplate GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendDrawTemplate> primary, DictionaryDataProvider<string, LegendDrawTemplate> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}