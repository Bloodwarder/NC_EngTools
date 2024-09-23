using NanocadUtilities;
using NetTopologySuite.Geometries;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GeoMod.GeometryExtensions
{
    // Методы расширения для NTS геометрий
    internal static class LineStringExtension
    {
        internal static Polyline ToDWGPolyline(this LineString lineString)
        {
            return ProcessGeometry(lineString);
        }

        internal static Polyline ProcessGeometry(Geometry geometry)
        {
            Polyline polyline = new()
            {
                LayerId = Workstation.Database.Clayer
            };
            Coordinate[] coordinates = geometry.Coordinates;
            for (int i = 0; i < coordinates.Length; i++)
            {
                polyline.AddVertexAt(i, new Point2d(coordinates[i].X, coordinates[i].Y), 0, 0, 0);
            }
            return polyline;
        }

    }
}
