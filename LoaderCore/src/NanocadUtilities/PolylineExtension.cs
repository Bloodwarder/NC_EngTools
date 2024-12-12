using Teigha.DatabaseServices;

namespace LoaderCore.NanocadUtilities
{
    public static class PolylineExtension
    {
        public static Polyline CopySourceProperties(this Polyline p, Polyline source)
        {
            p.Layer = source.Layer;
            p.ConstantWidth = source.ConstantWidth;
            p.LinetypeScale = source.LinetypeScale;
            p.LineWeight = source.LineWeight;
            p.Color = source.Color;
            return p;
        }
    }
}
