//System
//Microsoft
// Nanocad
//Internal
//NTS
using NetTopologySuite;

namespace GeoMod.NtsServices
{
    public interface INtsGeometryServicesFactory
    {
        public NtsGeometryServices Create();
    }
}
