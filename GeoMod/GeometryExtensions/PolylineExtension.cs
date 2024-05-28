using System.Collections.Generic;

using NetTopologySuite.Geometries;

using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace GeoMod.GeometryExtensions
{
    // Методы расширения для DWG геометрий

    // Предполагаю, что это можно было сделать изящнее, но реализовать интерфейс и переопределения для объектов закрытых сборок не умею

    internal static class PolylineExtension
    {
        internal static Geometry ToNTSGeometry(this Polyline polyline, GeometryFactory geometryFactory)
        {
            Coordinate[] coordinates = new Coordinate[polyline.NumberOfVertices];
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point2d p = polyline.GetPoint2dAt(i);
                coordinates[i] = new(p.X, p.Y);
            }
            if (polyline.Closed)
            {
                if (coordinates[0].Equals(coordinates[^1]))
                {
                    return geometryFactory.CreatePolygon(coordinates);
                }
                else
                {
                    List<Coordinate> coordsClosed = new(coordinates);
                    coordsClosed.Add(coordsClosed[0].Copy());
                    return geometryFactory.CreatePolygon(coordsClosed.ToArray());
                }
            }
            else
            {
                return geometryFactory.CreateLineString(coordinates);
            }
        }
    }
}
