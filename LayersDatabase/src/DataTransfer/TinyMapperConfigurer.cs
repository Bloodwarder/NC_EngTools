
using LayersIO.Model;
using Nelibur.ObjectMapper;

namespace LayersIO.DataTransfer
{
    internal static class TinyMapperConfigurer
    {
        private static bool IsConfigured { get; set; }
        private static readonly Dictionary<Type, Type> _mappedTypes = new();
        internal static void Configure()
        {
            if (IsConfigured)
                return;
            MapTypes<LayerPropertiesData, LayerProps>();
            MapTypes<LayerDrawTemplateData, LegendDrawTemplate>();
            MapTypes<LayerLegendData, LegendData>();
            IsConfigured = true;
        }

        internal static Type? GetMappedType(Type type)
        {
            return _mappedTypes.TryGetValue(type, out var mappedType) ? mappedType : null;
        }

        private static void MapTypes<T1, T2>()
        {
            TinyMapper.Bind(typeof(T1), typeof(T2));
            _mappedTypes[typeof(T1)] = typeof(T2);
            TinyMapper.Bind(typeof(T2), typeof(T1));
            _mappedTypes[typeof(T2)] = typeof(T1);
        }
    }


}
