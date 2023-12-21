using System.IO;
using LayerWorks.ModelspaceDraw;

namespace LayerWorks.ExternalData
{
    internal class LayerLegendDrawDictionary : ExternalDictionary<string, LegendDrawTemplate>
    {
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
                InstanceDictionary = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw")).GetDictionary();
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