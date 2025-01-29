using GeoMod.GeometryConverters;
using GeoMod.NtsServices;
using LoaderCore.Interfaces;
using Microsoft.Extensions.Configuration;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace GeoMod.Processing
{
    public class GeomodLabelRecognizer : IEntityPropertyRecognizer<Entity, string?>
    {
        private readonly double _searchBufferDistance;
        private readonly GeometryFactory _geometryFactory;


        public GeomodLabelRecognizer(IConfiguration configuration, INtsGeometryServicesFactory factory)
        {
            double labelHeight = configuration.GetValue<double>("UtilitiesConfiguration:DefaultLabelTextHeight");
            _searchBufferDistance = 1.6d * labelHeight;
            _geometryFactory = factory.Create().CreateGeometryFactory();
        }

        public string? RecognizeProperty(Entity entity)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Entity, string?> RecognizeProperty(IEnumerable<Entity> entities)
        {
            List<EntityGeometryFeaturePair> labels = new();
            List<EntityGeometryFeaturePair> features = new();
            Dictionary<Entity, string?> result = new();

            foreach (var entity in entities)
            {
                if (entity is MText mText)
                {
                    var egp = new EntityGeometryFeaturePair()
                    {
                        Entity = mText,
                        Geometry = new Point(mText.Location.X, mText.Location.Y),
                    };
                    labels.Add(egp);
                }
                else
                {
                    var egp = new EntityGeometryFeaturePair()
                    {
                        Entity = entity,
                        Geometry = EntityToGeometryConverter.TransferGeometry(entity, _geometryFactory)
                    };
                    if (egp.Geometry != null)
                        features.Add(egp);
                }
            }

            BufferParameters parameters = new()
            {
                EndCapStyle = EndCapStyle.Round,
                QuadrantSegments = 4,
                SimplifyFactor = 1,
                JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Round
            };

            foreach (var feature in features)
            {
                var buffer = feature.Geometry!.Buffer(_searchBufferDistance, parameters);
                var texts = labels.Where(l => buffer.Contains(l.Geometry)).Select(l => ((MText)l.Entity).Text).ToArray();
                if (texts.Any())
                {
                    result[feature.Entity] = string.Join(", ", texts);
                }
                else
                {
                    result[feature.Entity] = null;
                }
            }

            return result;
        }

        class EntityGeometryFeaturePair
        {
            public Geometry? Geometry { get; set; }
            public Entity Entity { get; set; }
        }
    }
}
