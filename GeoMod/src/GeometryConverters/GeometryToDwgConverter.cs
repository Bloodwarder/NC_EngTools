﻿using LoaderCore.Interfaces;
using LoaderCore;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Microsoft.Extensions.DependencyInjection;
using LoaderCore.NanocadUtilities;
using Teigha.Geometry;

namespace GeoMod.GeometryConverters
{
    internal static class GeometryToDwgConverter
    {
        internal static IEnumerable<Polyline> ToDWGPolylines(Geometry geometry)
        {
            List<Polyline> result = new();

            return geometry switch
            {
                LineString ls => new Polyline[1] { ToDWGPolyline(ls) },
                Polygon pg => ToDWGPolylines(pg),
                MultiPolygon mpg => mpg.Geometries.SelectMany(g => ToDWGPolylines((Polygon)g)),
                MultiLineString mls => mls.Geometries.Select(ls => ToDWGPolyline((LineString)ls)),
                _ => Array.Empty<Polyline>(),
            };
        }
        internal static Polyline ToDWGPolyline(LineString lineString)
        {
            Polyline polyline = new()
            {
                LayerId = Workstation.Database.Clayer
            };
            Coordinate[] coordinates = lineString.Coordinates;
            for (int i = 0; i < coordinates.Length; i++)
            {
                polyline.AddVertexAt(i, new Point2d(coordinates[i].X, coordinates[i].Y), 0, 0, 0);
            }

            return polyline.CheckClosedPolyline();
        }

        internal static IEnumerable<Polyline> ToDWGPolylines(Polygon polygon)
        {
            LinearRing? exteriorRing = (LinearRing)polygon.ExteriorRing;
            yield return ToDWGPolyline(exteriorRing).CheckClosedPolyline();
            foreach (LinearRing geom in polygon.InteriorRings.Cast<LinearRing>())
            {
                Polyline pl = ToDWGPolyline(geom).CheckClosedPolyline();
                pl.Closed = true;
                yield return pl;
            }
        }

        internal static Polyline CheckClosedPolyline(this Polyline polyline)
        {
            if (polyline.GetPoint2dAt(0) == polyline.GetPoint2dAt(polyline.NumberOfVertices - 1))
            {
                polyline.RemoveVertexAt(polyline.NumberOfVertices - 1);
                polyline.Closed = true;
            }
            return polyline;
        }
    }


}
