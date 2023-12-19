using System;
using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;


using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NtsBufferOps = NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Buffer;

using Teigha.Runtime;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using HostMgd.EditorInput;

using LoaderCore.Utilities;
using System.Text.RegularExpressions;

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

    internal static class BlockReferenceExtension
    {
        internal static Point ToNTSGeometry(this BlockReference blockRef, GeometryFactory geometryFactory)
        {
            Point point = geometryFactory.CreatePoint(new Coordinate(blockRef.Position.X, blockRef.Position.Y));
            return point;
        }
    }

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

    internal static class ClosedGeometryExtension
    {
        internal static Polyline ToDWGPolyline(this LinearRing linearRing)
        {
            Polyline polyline = LineStringExtension.ProcessGeometry(linearRing);
            polyline.Closed = true;
            return polyline;
        }

        internal static IEnumerable<Polyline> ToDWGPolylines(this Polygon polygon)
        {
            List<Polyline> polylines = new();
            LinearRing? exteriorRing = polygon.ExteriorRing as LinearRing;
            polylines.Add(exteriorRing!.ToDWGPolyline());
            foreach (LinearRing geom in polygon.InteriorRings.Cast<LinearRing>())
            {
                polylines.Add(geom.ToDWGPolyline());
            }
            return polylines;
        }

        internal static IEnumerable<Polyline> ToDWGPolylines(this MultiPolygon mPolygon)
        {
            List<Polyline> polylines = new();

            foreach (Polygon? polygon in mPolygon.Geometries.Select(g => g as Polygon))
            {
                polylines.AddRange(polygon!.ToDWGPolylines());
            }
            return polylines;
        }
        internal static IEnumerable<Polyline> ToDWGPolylines(this MultiLineString mLinestring)
        {
            List<Polyline> polylines = new();

            foreach (LineString? linestring in mLinestring.Geometries.Select(g => g as LineString))
            {
                polylines.AddRange(linestring!.ToDWGPolylines());
            }
            return polylines;
        }

        internal static IEnumerable<Polyline> ToDWGPolylines(this Geometry geometry)
        {
            Type type = geometry.GetType();
            List<Polyline> polylines = new();

            switch (type.Name.ToUpper())
            {
                case "LINESTRING":
                    LineString? ls = geometry as LineString;
                    polylines.Add(ls!.ToDWGPolyline());
                    break;
                case "LINEARRING":
                    LinearRing? lr = geometry as LinearRing;
                    polylines.Add(lr!.ToDWGPolyline());
                    break;
                case "POLYGON":
                    Polygon? pg = geometry! as Polygon;
                    polylines.AddRange(pg!.ToDWGPolylines());
                    break;
                case "MULTIPOLYGON":
                    MultiPolygon? mpg = geometry! as MultiPolygon;
                    polylines.AddRange(mpg!.ToDWGPolylines());
                    break;
                case "MULTILINESTRING":
                    MultiLineString? mls = geometry! as MultiLineString;
                    polylines.AddRange(mls!.ToDWGPolylines());
                    break;
                default:
                    break;
            }
            return polylines;
        }
    }
}
