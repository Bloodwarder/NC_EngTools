//System
using System.Collections.Generic;
using System.Linq;
//Microsoft
using Microsoft.Extensions.Logging;
// Nanocad
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
//Internal
using LoaderCore.NanocadUtilities;
using GeoMod.GeometryConverters;
//NTS
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using GeoMod.NtsServices;

namespace GeoMod.Commands
{

    /// <summary>
    /// Класс, содержащий гео-команды и вспомогательные данные для их функционирования
    /// </summary>
    public class GeomodPrecisionCommands
    {
        private readonly NtsGeometryServices _geometryServices;
        private readonly IPrecisionReducer _reducer;


        static GeomodPrecisionCommands() { }

        public GeomodPrecisionCommands(IPrecisionReducer reducer, INtsGeometryServicesFactory geometryServicesFactory)
        {
            _geometryServices = geometryServicesFactory.Create();
            _reducer = reducer;
        }


        // TODO: реализовать добавление точек при частичном совпадении сторон полигонов
        public void ReduceCoordinatePrecision()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить выбранные объекты, отфильтровать полилинии и создать из них nts полигоны
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var polylines = (from ObjectId id in entitiesIds
                                 let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                 where entity is Polyline pl
                                 select entity as Polyline).ToArray();
                // Вывести предупреждения о необработанных объектах
                if (polylines.Length == 0)
                {
                    Workstation.Logger?.LogInformation("Нет полилиний в наборе");
                    return;
                }
                if (polylines.Length < entitiesIds.Length)
                    Workstation.Logger?.LogInformation("Не обработано {UnprocessedNumber} объектов, не являющихся полилиниями", entitiesIds.Length - polylines.Length);

                // Провести валидацию геометрии и уменьшить точность координат, при этом сохраняя связь с исходными полилиниями
                Dictionary<Geometry, Polyline> geometries = polylines.Select(p => (p, p.ToNtsGeometry(geometryFactory)))
                                                                      .Select(t => (t.p, t.Item2.IsValid ? t.Item2 : GeometryFixer.Fix(t.Item2)))
                                                                      .Select(t => (t.p, _reducer.Reduce(t.Item2)))
                                                                      .ToDictionary(t => t.Item2, t => t.p);

                // Объединить геометрию, создать из неё полилинии и поместить в модель
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // получить все полилинии, сохраняя свойства исходных полилиний
                geometries.Keys.SelectMany(g => GeometryToDwgConverter.ToDWGPolylines(g).Select(p => p.CopySourceProperties(geometries[g])))
                               .ToList()
                               .ForEach(pl => modelSpace!.AppendEntity(pl));

                // Удалить исходные полилинии
                foreach (Polyline pl in polylines)
                    pl.Erase();

                transaction.Commit();
            }
        }
    }
}
