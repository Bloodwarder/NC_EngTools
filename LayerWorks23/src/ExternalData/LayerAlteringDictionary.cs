using System.IO;

namespace LayerWorks.ExternalData
{
    internal class LayerAlteringDictionary : ExternalDictionary<string, string>
    {
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
                InstanceDictionary = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Alter);
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