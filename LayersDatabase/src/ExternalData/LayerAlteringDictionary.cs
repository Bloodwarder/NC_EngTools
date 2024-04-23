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
                InstanceDictionary = new XmlLayerDataProvider<string, string>(PathProvider.GetPath(XmlAlterName)).GetData();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Alter);
            }
        }

        public static bool TryGetValue(string layername, out string? success)
        {
            return instance.TryGetInstanceValue(layername, out success);
        }
        public static void Reload(ILayerDataWriter<string, string> primary, ILayerDataProvider<string, string> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }
}