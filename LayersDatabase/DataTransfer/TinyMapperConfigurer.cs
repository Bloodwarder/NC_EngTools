
using LayersDatabase.Model;
using LayerWorks.ExternalData;
using LayerWorks.Legend;
using LayerWorks.ModelspaceDraw;
using Nelibur.ObjectMapper;

namespace LayersDatabase.DataTransfer
{
    internal static class TinyMapperConfigurer
    {
        static TinyMapperConfigurer() 
        {
            TinyMapper.Bind<LayerPropertiesData, LayerProps>();
            TinyMapper.Bind<LayerDrawTemplateData, LegendDrawTemplate>();
            TinyMapper.Bind<LayerLegendData, LegendData>();
        }
    }
}
