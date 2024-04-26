﻿//System
using System.IO;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Colors;

//internal modules
using LoaderCore.Utilities;
using LayerWorks.LayerProcessing;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using NameClassifiers;
using NanocadUtilities;

namespace LayerWorks.LayerProcessing
{

    static class LayerChecker
    {
        internal static event EventHandler LayerAddedEvent;
        internal static void Check(string layername)
        {

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt.Has(layername))
                    {
                        bool propsgetsuccess = LayerPropertiesDictionary.TryGetValue(layername, out LayerProps lp);
                        LayerTableRecord ltRecord = AddLayer(layername, lp);

                        //Process new layer if isolated chapter visualization is active
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, new EventArgs());

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

        internal static void Check(LayerWrapper layer)
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt.Has(layer.LayerInfo.Name))
                    {
                        bool propsgetsuccess = LayerPropertiesDictionary.TryGetValue(layer.LayerInfo.TrueName, out LayerProps lp);
                        LayerTableRecord ltRecord = AddLayer(layer.LayerInfo.Name, lp);

                        //Process new layer if isolated chapter visualization is active
                        EventArgs e = new EventArgs();
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

        private static LayerTableRecord AddLayer(string layername, LayerProps lp)
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            Database database = Workstation.Database;
            ObjectId linetypeRecordId = FindLinetype(lp.LineTypeName, out bool ltgetsuccess);
            if (!ltgetsuccess)
            {
                string str = $"Не найден тип линий для слоя {layername}. Назначен тип линий Continious";
                Workstation.Editor.WriteMessage(str);
            }
            LayerTableRecord ltRecord = new LayerTableRecord
            {
                Name = layername,
                Color = Color.FromRgb(lp.Red, lp.Green, lp.Blue),
                LineWeight = (LineWeight)lp.LineWeight,
                LinetypeObjectId = linetypeRecordId
                //Transparency = new Teigha.Colors.Transparency(Teigha.Colors.TransparencyMethod.ByAlpha)
            };
            LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForWrite) as LayerTable;
            layerTable.Add(ltRecord);
            transaction.AddNewlyCreatedDBObject(ltRecord, true);
            return ltRecord;
        }

        internal static ObjectId FindLinetype(string linetypename, out bool ltgetsuccess)
        {
            TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;
            LinetypeTable ltt = tm.TopTransaction.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false) as LinetypeTable;
            ltgetsuccess = true;
            if (!ltt.Has(linetypename))
            {
                FileInfo fi = new FileInfo(PathProvider.GetPath("STANDARD1.lin"));
                try
                {
                    db.LoadLineTypeFile(linetypename, fi.FullName);
                }
                catch
                {
                    ltgetsuccess = false;
                    return SymbolUtilityServices.GetLinetypeContinuousId(db);
                }
            }
            return ltt[linetypename];
        }

    }

}






