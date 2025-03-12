using GeoMod.GeometryConverters;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using Teigha.DatabaseServices;
//System
namespace GeoMod.GeometryConverters
{
    internal static class EntityToGeometryConverter
    {
        /// <summary>
        /// Преобразовать коллекцию dwg геометрии в коллекцию nts геометрии
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public static IEnumerable<Geometry> TransferGeometry(IEnumerable<Entity> entities, GeometryFactory geometryFactory)
        {
            List<Geometry> geometries = new();
            bool warningShow = false;
            foreach (Entity entity in entities)
            {
                switch (entity)
                {
                    case Polyline pl:
                        if (pl.HasBulges)
                            warningShow = true;
                        geometries.Add(pl.ToNtsGeometry(geometryFactory));
                        break;
                    case BlockReference bref:
                        geometries.Add(bref.ToNTSGeometry(geometryFactory));
                        break;
                    case Circle circle:
                        geometries.Add(circle.ToNTSGeometry(geometryFactory));
                        break;
                    default:
                        continue;
                }
            }
            if (warningShow)
                Workstation.Logger?.LogWarning("Внимание! Кривые не поддерживаются. Для корректного вывода аппроксимируйте геометрию с дуговыми сегментами");
            return geometries;
        }

        /// <summary>
        /// Преобразовать dwg геометрию в nts геометрию
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public static Geometry? TransferGeometry(Entity entity, GeometryFactory geometryFactory)
        {
            return entity switch
            {
                Polyline pl => pl.ToNtsGeometry(geometryFactory),
                BlockReference bref => bref.ToNTSGeometry(geometryFactory),
                Circle circle => circle.ToNTSGeometry(geometryFactory),
                _ => null,
            };
        }

    }
}