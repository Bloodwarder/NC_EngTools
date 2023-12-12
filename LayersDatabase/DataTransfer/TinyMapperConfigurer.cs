
using LayersDatabase.Model;
using LayerWorks.ExternalData;
using LayerWorks.ModelspaceDraw;
using Nelibur.ObjectMapper;

namespace LayersDatabase.DataTransfer
{
    internal static class TinyMapperConfigurer
    {
        static TinyMapperConfigurer() 
        {
            TinyMapper.Bind<LayerData, LayerProps>();
            TinyMapper.Bind<LayerData, LegendDrawTemplate>();
        }
    }
}
