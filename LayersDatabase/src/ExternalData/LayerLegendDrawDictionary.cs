using LayersIO.DataTransfer;
using LayersIO.Xml;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;

namespace LayersIO.ExternalData
{
    public class LayerLegendDrawDictionary : ExternalDictionary<string, LegendDrawTemplate>
    {
        const string XmlLegendDrawName = "Layer_LegendDraw.xml";
        static LayerLegendDrawDictionary() { }
        LayerLegendDrawDictionary()
        {
            try
            {
                InstanceDictionary = new XmlLayerDataProvider<string, LegendDrawTemplate>(PathProvider.GetPath(XmlLegendDrawName)).GetData();
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