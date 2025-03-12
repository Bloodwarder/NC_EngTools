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
    /// �����, ���������� ���-������� � ��������������� ������ ��� �� ����������������
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


        // TODO: ����������� ���������� ����� ��� ��������� ���������� ������ ���������
        public void ReduceCoordinatePrecision()
        {
            var geometryFactory = _geometryServices.CreateGeometryFactory();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // �������� ��������� �������, ������������� ��������� � ������� �� ��� nts ��������
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                ObjectId[] entitiesIds = psr.Value.GetObjectIds();
                var polylines = (from ObjectId id in entitiesIds
                                 let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                 where entity is Polyline pl
                                 select entity as Polyline).ToArray();
                // ������� �������������� � �������������� ��������
                if (polylines.Length == 0)
                {
                    Workstation.Logger?.LogInformation("��� ��������� � ������");
                    return;
                }
                if (polylines.Length < entitiesIds.Length)
                    Workstation.Logger?.LogInformation("�� ���������� {UnprocessedNumber} ��������, �� ���������� �����������", entitiesIds.Length - polylines.Length);

                // �������� ��������� ��������� � ��������� �������� ���������, ��� ���� �������� ����� � ��������� �����������
                Dictionary<Geometry, Polyline> geometries = polylines.Select(p => (p, p.ToNtsGeometry(geometryFactory)))
                                                                      .Select(t => (t.p, t.Item2.IsValid ? t.Item2 : GeometryFixer.Fix(t.Item2)))
                                                                      .Select(t => (t.p, _reducer.Reduce(t.Item2)))
                                                                      .ToDictionary(t => t.Item2, t => t.p);

                // ���������� ���������, ������� �� �� ��������� � ��������� � ������
                BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // �������� ��� ���������, �������� �������� �������� ���������
                geometries.Keys.SelectMany(g => GeometryToDwgConverter.ToDWGPolylines(g).Select(p => p.CopySourceProperties(geometries[g])))
                               .ToList()
                               .ForEach(pl => modelSpace!.AppendEntity(pl));

                // ������� �������� ���������
                foreach (Polyline pl in polylines)
                    pl.Erase();

                transaction.Commit();
            }
        }
    }
}
