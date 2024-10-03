using LoaderCore.Interfaces;
using LoaderCore;
using NanocadUtilities;
using NetTopologySuite.Geometries;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Microsoft.Extensions.DependencyInjection;

namespace GeoMod.GeometryExtensions
{
    // Методы расширения для NTS геометрий
    internal static class LineStringExtension
    {
        private static IEntityFormatter? _formatter = NcetCore.ServiceProvider.GetService<IEntityFormatter>();

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
            _formatter?.FormatEntity(polyline);
            return polyline;
        }

    }
}
