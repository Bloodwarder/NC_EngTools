
using LayersIO.Model;
using LayersIO.DataTransfer;
using Nelibur.ObjectMapper;

namespace LayersIO.DataTransfer
{
    internal static class TinyMapperConfigurer
    {
        private static bool _isConfigured { get; set; }
        private static Dictionary<Type, Type> _mappedTypes = new();
        internal static void Configure()
        {
            if (_isConfigured)
                return;
            MapTypes<LayerPropertiesData, LayerProps>();
            MapTypes<LayerDrawTemplateData, LegendDrawTemplate>();
            MapTypes<LayerLegendData, LegendData>();
            _isConfigured = true;
        }

        internal static Type? GetMappedType(Type type)
        {
            return _mappedTypes.TryGetValue(type, out var mappedType) ? mappedType : null;
        }

        private static void MapTypes<T1,T2>()
        {
            TinyMapper.Bind(typeof(T1), typeof(T2));
            _mappedTypes[typeof(T1)] = typeof(T2);
            TinyMapper.Bind(typeof(T2), typeof(T1));
            _mappedTypes[typeof(T2)] = typeof(T1);
        }
    }


}
