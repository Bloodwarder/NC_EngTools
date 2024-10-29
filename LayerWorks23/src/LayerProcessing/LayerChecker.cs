//System
using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
//internal modules
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
using System.IO;
using Teigha.Colors;
//nanoCAD
using Teigha.DatabaseServices;
using Microsoft.Extensions.Logging;

namespace LayerWorks.LayerProcessing
{
    static class LayerChecker
    {
        private const string LineTypeFileName = "STANDARD1.lin";

        internal static event EventHandler? LayerAddedEvent;

        /// <summary>
        /// Ищет в таблице слоёв слой с именем, пропущенным через NameParser, и при его отсутствии добавляет его, 
        /// используя репозиторий с данными стандартных слоёв
        /// </summary>
        /// <param name="layerInfo"></param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        internal static ObjectId Check(LayerInfo layerInfo)
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    string layerName = layerInfo.Name;
                    if (!lt!.Has(layerName))
                    {
                        Workstation.Logger?.LogDebug("{ProcessingObject}: Слоя {LayerName} нет в таблице слоёв. Попытка создания слоя", nameof(LayerChecker), layerName);

                        var standardService = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
                        bool propsGetSuccess = standardService.TryGet(layerInfo.TrueName, out LayerProps? props);
                        LayerTableRecord ltRecord = AddLayer(layerInfo.Name, props);

                        //Process new layer if isolated chapter visualization is active
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, new());

                        Workstation.Logger?.LogDebug("{ProcessingObject}: Слой {LayerName} успешно добавлен", nameof(LayerChecker), layerName);
                        return ltRecord.ObjectId;
                    }
                    else
                    {
                        Workstation.Logger?.LogDebug("{ProcessingObject}: Слой {LayerName} успешно добавлен", nameof(LayerChecker), layerName);
                        return lt[layerInfo.Name];
                    }
                }
                catch (NoPropertiesException ex)
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: {Message}", nameof(LayerChecker), ex.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Ищет в таблице слоёв слой с указанным именем и при его отсутствии добавляет его, 
        /// используя репозиторий с данными стандартных слоёв
        /// </summary>
        /// <param name="layername"></param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        internal static ObjectId Check(string layername)
        {
            try
            {
                var layerInfoResult = NameParser.Current.GetLayerInfo(layername);
                if (layerInfoResult.Status == LayerInfoParseStatus.Success)
                {
                    return Check(layerInfoResult.Value);
                }
                else
                {
                    throw layerInfoResult.GetExceptions().First();
                }
            }
            catch (NoPropertiesException ex)
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: {Message}", nameof(LayerChecker), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Ищет в таблице слоёв слой, связанный с объектом  с указанным именем и при его отсутствии добавляет его, 
        /// используя репозиторий с данными стандартных слоёв
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        internal static ObjectId Check(LayerWrapper layer) => Check(layer.LayerInfo);

        internal static ObjectId ForceCheck(string layerName)
        {
            LayerTable layerTable = (LayerTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForWrite);
            if (!layerTable.Has(layerName))
                return AddLayer(layerName, null).Id;
            else
                return layerTable[layerName];
        }

        private static LayerTableRecord AddLayer(string layername, LayerProps? lp)
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            Database database = Workstation.Database;

            bool ltgetsuccess = TryFindLinetype(lp?.LinetypeName!, out ObjectId linetypeRecordId);
            if (!ltgetsuccess)
            {
                Workstation.Logger?.LogInformation("Не найден тип линий для слоя {LayerName}. Назначен тип линий Continious", layername);
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
                FileInfo fi = new(PathProvider.GetPath(LineTypeFileName));
                try
                {
                    Workstation.Database.LoadLineTypeFile(linetypename, fi.FullName);
                }
                catch
                {
                    lineTypeId = defaultLinetypeId;
                    Workstation.Logger?.LogWarning("Ошибка чтения файла типов линий. Назначен тип линий по умолчанию");
                    return false;
                }
            }
            lineTypeId = ltt[linetypename];
            return true;
        }

    }

}






