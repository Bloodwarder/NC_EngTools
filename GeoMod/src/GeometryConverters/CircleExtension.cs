using NetTopologySuite.Geometries;
using Teigha.DatabaseServices;

namespace GeoMod.GeometryConverters
{
    internal static class CircleExtension
    {
        internal static Geometry ToNTSGeometry(this Circle circle, GeometryFactory geometryFactory)
        {
            Geometry circleGeometry = new Point(circle.Center.X, circle.Center.Y).Buffer(circle.Radius);
            return circleGeometry;
        }
    }
}
