using GeoMod.GeometryConverters;
using GeoMod.NtsServices;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using System;
using System.Collections.Generic;
using Teigha.DatabaseServices;

namespace GeoMod.Processing
{
    internal class GeomodNtsBufferizer : IBuffer
    {
        private readonly ILayerChecker? _layerChecker;
        private readonly NtsGeometryServices _geometryServices;
        private readonly DynamicRoundBufferParametersProvider _bufferParametersProvider; 

        public GeomodNtsBufferizer(ILayerChecker? layerChecker, INtsGeometryServicesFactory factory)
        {
            _layerChecker = layerChecker;
            _geometryServices = factory.Create();
            _bufferParametersProvider = new(1d, 4, 30d, 16); // TODO: сделать провайдер параметров сервисом, числа перенести в конфигурацию
        }


        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, double width)
        {
            var polylines = ProcessGeometry(entities, width);
            ObjectId layerId = Workstation.Database.Clayer;
            foreach (var polyline in polylines)
            {
                polyline.LayerId = layerId;
                yield return polyline;
            }
        }

        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, double width, string layerName)
        {
            var polylines = ProcessGeometry(entities, width);
            ObjectId layerId = _layerChecker?.Check(layerName) ?? Workstation.Database.Clayer;
            foreach (var polyline in polylines)
            {
                polyline.LayerId = layerId;
                yield return polyline;
            }
        }

        public IEnumerable<Polyline> Buffer(IEnumerable<Entity> entities, Dictionary<string, double> widthDictionary, string layerName)
        {
            var polylines = ProcessGeometry(entities, widthDictionary);
            ObjectId layerId = _layerChecker?.Check(layerName) ?? Workstation.Database.Clayer;
            foreach (var polyline in polylines)
            {
                polyline.LayerId = layerId;
                yield return polyline;
            }
        }

        private IEnumerable<Polyline> ProcessGeometry(IEnumerable<Entity> entities, double width)
        {
            return ProcessGeometry(entities, s => width);
        }

        private IEnumerable<Polyline> ProcessGeometry(IEnumerable<Entity> entities, Dictionary<string, double> widthDictionary)
        {
            return ProcessGeometry(entities, s => widthDictionary[s]);
        }

        private IEnumerable<Polyline> ProcessGeometry(IEnumerable<Entity> entities, Func<string, double> func)
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();
            List<Geometry> bufferGeoms = new();
            foreach (var entity in entities) 
            {
                Geometry? sourceGeom = EntityToGeometryConverter.TransferGeometry(entity, geometryFactory);
                if (sourceGeom == null)
                    continue;
                Geometry fixedGeometry = GeometryFixer.Fix(sourceGeom);
                double width = func(entity.Layer);
                Geometry bufferGeom = fixedGeometry.Buffer(width, _bufferParametersProvider.GetBufferParameters(width));
                bufferGeoms.Add(bufferGeom);
            }
            Geometry buffer = geometryFactory.CreateGeometryCollection(bufferGeoms.ToArray()).Union();
            return GeometryToDwgConverter.ToDWGPolylines(buffer);
        }
    }
}
