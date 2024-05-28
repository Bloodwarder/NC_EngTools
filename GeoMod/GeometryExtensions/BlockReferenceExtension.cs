using NetTopologySuite.Geometries;

using Teigha.DatabaseServices;

namespace GeoMod.GeometryExtensions
{
    internal static class BlockReferenceExtension
    {
        internal static Point ToNTSGeometry(this BlockReference blockRef, GeometryFactory geometryFactory)
        {
            Point point = geometryFactory.CreatePoint(new Coordinate(blockRef.Position.X, blockRef.Position.Y));
            return point;
        }
    }
}
