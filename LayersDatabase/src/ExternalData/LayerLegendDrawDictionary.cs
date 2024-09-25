using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace LayersIO.ExternalData
{
    public class LayerLegendDrawDictionary : ExternalRepository<string, LegendDrawTemplate>
    {
        const string XmlLegendDrawName = "Layer_LegendDraw.xml";
        static LayerLegendDrawDictionary() { }
        LayerLegendDrawDictionary()
        {
            try
            {
                //var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IDataProviderFactory<string, LegendDrawTemplate>>();
                //InstanceDictionary = service.CreateProvider(PathProvider.GetPath(XmlLegendDrawName)).GetData();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.LegendDraw);
            }
        }
        public void Reload(ILayerDataWriter<string, LegendDrawTemplate> primary, ILayerDataProvider<string, LegendDrawTemplate> secondary)
        {
            ReloadInstance(primary, secondary);
        }
    }
}