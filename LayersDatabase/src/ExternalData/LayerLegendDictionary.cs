using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.ExternalData
{
    public class LayerLegendDictionary : ExternalDictionary<string, LegendData>
    {
        const string XmlLegendName = "Layer_Legend.xml";

        static LayerLegendDictionary() { }
        LayerLegendDictionary()
        {
            try
            {
                var service = LoaderCore.LoaderExtension.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LegendData>>();
                InstanceDictionary = service.CreateProvider(PathProvider.GetPath(XmlLegendName)).GetData();
            }
            catch (FileNotFoundException)
            {
                //ExternalDataLoader.Reloader(ToReload.Legend);
            }
        }
        public void Reload(ILayerDataWriter<string, LegendData> primary, ILayerDataProvider<string, LegendData> secondary)
        {
            ReloadInstance(primary, secondary);
        }
    }
}