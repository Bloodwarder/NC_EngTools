using System;
using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;

using NetTopologySuite.Geometries;

using Teigha.DatabaseServices;

namespace GeoMod.GeometryExtensions
{

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
