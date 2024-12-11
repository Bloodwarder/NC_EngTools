//System
//Microsoft
// Nanocad
//Internal
//NTS
using NetTopologySuite.Geometries;

namespace GeoMod.NtsServices
{
    public interface IPrecisionReducer
    {
        public Geometry Reduce(Geometry geometry);
    }
}
