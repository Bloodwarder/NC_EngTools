
using LayersIO.Model;
using LayersIO.DataTransfer;
using Nelibur.ObjectMapper;

namespace LayersIO.DataTransfer
{
    internal static class TinyMapperConfigurer
    {
        private static bool _isConfigured { get; set; }
        internal static void Configure() 
        {
            if (_isConfigured)
                return;
            TinyMapper.Bind<LayerPropertiesData, LayerProps>();
            TinyMapper.Bind<LayerDrawTemplateData, LegendDrawTemplate>();
            TinyMapper.Bind<LayerLegendData, LegendData>();
            TinyMapper.Bind<LayerProps, LayerPropertiesData>();
            TinyMapper.Bind<LegendDrawTemplate, LayerDrawTemplateData>();
            TinyMapper.Bind<LegendData, LayerLegendData>();
            _isConfigured = true;
        }
    }
}
