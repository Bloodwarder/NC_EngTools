
using LayersIO.Model;
using LayersIO.DataTransfer;
using Nelibur.ObjectMapper;

namespace LayersIO.DataTransfer
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
