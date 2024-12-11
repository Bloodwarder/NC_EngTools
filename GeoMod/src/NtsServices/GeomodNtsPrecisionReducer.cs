//System
//Microsoft
using Microsoft.Extensions.Configuration;
// Nanocad
//Internal
//NTS
using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;

namespace GeoMod.NtsServices
{
    public class GeomodNtsPrecisionReducer : IPrecisionReducer
    {
        private const string RelatedConfigurationSection = "GeoModConfiguration";

        private GeometryPrecisionReducer _reducer;
        public GeomodNtsPrecisionReducer(IConfiguration configuration)
        {
            var section = configuration.GetRequiredSection(RelatedConfigurationSection);
            double reducedPrecision = section.GetValue<double>("ReducedPrecision");
            PrecisionModel precisionModel = new(reducedPrecision);
            _reducer = new GeometryPrecisionReducer(precisionModel);
        }

        public Geometry Reduce(Geometry geometry)
        {
            return _reducer.Reduce(geometry);
        }
    }
}
