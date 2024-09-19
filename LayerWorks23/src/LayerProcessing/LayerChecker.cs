//System
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LoaderCore.Interfaces;
//internal modules
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using NanocadUtilities;
using System.IO;
using Teigha.Colors;
//nanoCAD
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{

    static class LayerChecker
    {
        internal static event EventHandler? LayerAddedEvent;
        internal static void Check(string layername)
        {

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt!.Has(layername))
                    {
                        var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IStandardReader<LayerProps>>();
                        bool propsgetsuccess = service.TryGetStandard(layername, out LayerProps? lp);

                        LayerTableRecord ltRecord = AddLayer(layername, lp);

                        //Process new layer if isolated chapter visualization is active
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, new EventArgs());

                    }
                    else
                    {
                        transaction.Commit();
                        return;
                    }
                }
                catch (NoPropertiesException)
                {
                    throw new NoPropertiesException("Проверка слоя не удалась");
                }
            }
        }

        internal static void Check(LayerWrapper layer)
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt!.Has(layer.LayerInfo.Name))
                    {
                        var standardService = LoaderCore.NcetCore.ServiceProvider.GetService<IStandardReader<LayerProps>>()!;
                        bool propsgetsuccess = standardService.TryGetStandard(layer.LayerInfo.TrueName, out LayerProps? props);
                        LayerTableRecord ltRecord = AddLayer(layer.LayerInfo.Name, props);

                        //Process new layer if isolated chapter visualization is active
                        EventArgs e = new();
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, e);

                    }
                    else
                    {
                        return;
                    }
                }
                catch (NoPropertiesException)
                {
                    throw new NoPropertiesException("Проверка слоя не удалась");
                }
            }
        }

        private static LayerTableRecord AddLayer(string layername, LayerProps? lp)
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            Database database = Workstation.Database;

            bool ltgetsuccess = TryFindLinetype(lp?.LineTypeName!, out ObjectId linetypeRecordId);
            if (!ltgetsuccess)
            {
                string str = $"Не найден тип линий для слоя {layername}. Назначен тип линий Continious";
                Workstation.Editor.WriteMessage(str);
            }
            LayerTableRecord ltRecord = new()
            {
                Name = layername,
                Color = Color.FromRgb(lp?.Red ?? 0, lp?.Green ?? 0, lp?.Blue ?? 0),
                LineWeight = (LineWeight?)lp?.LineWeight ?? LineWeight.ByLineWeightDefault,
                LinetypeObjectId = linetypeRecordId
            };
            LayerTable? layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForWrite) as LayerTable;
            layerTable!.Add(ltRecord);
            transaction.AddNewlyCreatedDBObject(ltRecord, true);
            return ltRecord;
        }

        internal static bool TryFindLinetype(string? linetypename, out ObjectId lineTypeId)
        {

            ObjectId defaultLinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            if (linetypename == null)
            {
                lineTypeId = defaultLinetypeId;
                return false;
            }
            TransactionManager tm = Workstation.TransactionManager;
            LinetypeTable? ltt = tm.TopTransaction.GetObject(Workstation.Database.LinetypeTableId, OpenMode.ForWrite, false) as LinetypeTable;
            if (!ltt!.Has(linetypename))
            {
                FileInfo fi = new(PathProvider.GetPath("STANDARD1.lin"));
                try
                {
                    Workstation.Database.LoadLineTypeFile(linetypename, fi.FullName);
                }
                catch
                {
                    lineTypeId = defaultLinetypeId;
                    Workstation.Editor.WriteMessage("Ошибка чтения файла типов линий. Назначен тип линий по умолчанию");
                    return false;
                }
            }
            lineTypeId = ltt[linetypename];
            return true;
        }

    }

}






