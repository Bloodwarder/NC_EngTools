using Microsoft.Extensions.DependencyInjection;
using LayersIO.Xml;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;

namespace LayersIO.ExternalData
{
    public class LayerAlteringDictionary : ExternalDictionary<string, string>
    {
        const string XmlAlterName = "Layer_Alter.xml";
        static LayerAlteringDictionary() { }
        private LayerAlteringDictionary()
        {
            try
            {
                var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, string>>();
                InstanceDictionary = service.CreateProvider(PathProvider.GetPath(XmlAlterName)).GetData();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Alter);
            }
        }

        public void Reload(ILayerDataWriter<string, string> primary, ILayerDataProvider<string, string> secondary)
        {
            ReloadInstance(primary, secondary);
        }
    }
}