using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LoaderCore.NanocadUtilities
{
    public static class HatchExtension
    {
        public static void AssingnLoop(this Hatch hatch, IEnumerable<Polyline> polylines)
        {
            try
            {
                var ids = polylines.Select(p => p.Id).ToArray();
                var collection = new ObjectIdCollection(ids);
                hatch.AppendLoop(HatchLoopTypes.Polyline, collection);
            }
            catch
            {
                hatch.AssignLoopByVerticesAndBulges(polylines);
            }
        }

        public static void AssignLoopByVerticesAndBulges(this Hatch hatch, IEnumerable<Polyline> polylines)
        {
            foreach (Polyline pl in polylines)
            {
                Point2dCollection vertexCollection = new(pl.NumberOfVertices);
                DoubleCollection bulgesCollection = new(pl.NumberOfVertices);
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    vertexCollection.Add(pl.GetPoint2dAt(i));
                    bulgesCollection.Add(pl.GetBulgeAt(i));
                }
                hatch.AppendLoop(HatchLoopTypes.Polyline, vertexCollection, bulgesCollection);
            }
        }
    }
}
