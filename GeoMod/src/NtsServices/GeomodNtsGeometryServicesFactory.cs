//System
//Microsoft
using Microsoft.Extensions.Configuration;
// Nanocad
//Internal
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace GeoMod.NtsServices
{
    /// <summary>
    /// Инициализирует статический объект (синглтон) NtsGeometryServices на основе конфигурации и в дальнейшем возвращает его.
    /// </summary>
    public class GeomodNtsGeometryServicesFactory : INtsGeometryServicesFactory
    {
        private const string RelatedConfigurationSection = "GeoModConfiguration";
        public GeomodNtsGeometryServicesFactory(IConfiguration configuration)
        {
            var section = configuration.GetRequiredSection(RelatedConfigurationSection);
            double precision = section.GetValue<double>("Precision");
            NtsGeometryServices.Instance = new NtsGeometryServices( // default CoordinateSequenceFactory
                                                        NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                                                        // default precision model
                                                        new PrecisionModel(precision),
                                                        // default SRID
                                                        -1,
                                                        // Geometry overlay operation function set to use (Legacy or NG)
                                                        GeometryOverlay.NG,
                                                        // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
                                                        new CoordinateEqualityComparer()
                                                       );
        }

        public NtsGeometryServices Create()
        {
            return NtsGeometryServices.Instance;
        }
    }
}
