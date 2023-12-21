using LoaderCore.Utilities;
using LayersIO.Xml;
using System.Diagnostics;

namespace LayersIO.ExternalData
{
    public class LayerAlteringDictionary : ExternalDictionary<string, string>
    {
        const string XmlAlterName = "Layer_Alter.xml";

        private static readonly LayerAlteringDictionary instance;
        static LayerAlteringDictionary()
        {
            if (instance == null)
                instance = new LayerAlteringDictionary();
        }
        private LayerAlteringDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, string>(PathProvider.GetPath(XmlAlterName)).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Alter);
            }
        }

        public static string GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, string> primary, DictionaryDataProvider<string, string> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}