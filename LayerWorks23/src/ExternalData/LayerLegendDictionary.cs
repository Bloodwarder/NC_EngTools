using System.IO;
using LayerWorks.Legend;

namespace LayerWorks.ExternalData
{
    internal class LayerLegendDictionary : ExternalDictionary<string, LegendData>
    {
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
                InstanceDictionary = new XmlDictionaryDataProvider<string, LegendData>(PathOrganizer.GetPath("Legend")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Legend);
            }
        }
        public static LegendData GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendData> primary, DictionaryDataProvider<string, LegendData> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}