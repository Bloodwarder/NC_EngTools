using System.Linq;
using Teigha.Runtime;
using Teigha.DatabaseServices;
using LoaderCore.Utilities;
using LayerWorks.LayerProcessing;
using LayersIO.DataTransfer;
using LayersIO.Excel;
using LayersIO.Xml;

namespace LayersIO.ExternalData
{
    /// <summary>
    /// Загрузчик данных
    /// </summary>
    public static class ExternalDataLoader
    {
        /// <summary>
        /// Команда для перезагрузки словарей с данными
        /// </summary>
        [CommandMethod("RELOADPROPS")]
        public static void ReloadDictionaries()
        {
            Workstation.Define();
            Workstation.Editor.WriteMessage("Начало импорта данных. Подождите");
            Reloader(ToReload.Properties | ToReload.Alter | ToReload.Legend | ToReload.LegendDraw);
            Workstation.Editor.WriteMessage("Импорт данных завершён");
        }

        /// <summary>
        /// Перезагрузка словарей с данными
        /// </summary>
        /// <param name="reload">Словари к перезагрузке</param>
        public static void Reloader(ToReload reload)
        {
            if ((reload & ToReload.Properties) == ToReload.Properties)
            {
                ExcelStructDictionaryDataProvider<string, LayerProps> xlpropsprovider = new ExcelStructDictionaryDataProvider<string, LayerProps>(PathProvider.GetPath("Layer_Props.xlsm"), "Props");
                XmlDictionaryDataProvider<string, LayerProps> xmlpropsprovider = new XmlDictionaryDataProvider<string, LayerProps>(PathProvider.GetPath("Layer_Props.xml"));
                LayerPropertiesDictionary.Reload(xmlpropsprovider, xlpropsprovider);
            }
            if ((reload & ToReload.Alter) == ToReload.Alter)
            {
                ExcelSimpleDictionaryDataProvider<string, string> xlalterprovider = new ExcelSimpleDictionaryDataProvider<string, string>(PathProvider.GetPath("Layer_Props.xlsm"), "Alter");
                XmlDictionaryDataProvider<string, string> xmlalterprovider = new XmlDictionaryDataProvider<string, string>(PathProvider.GetPath("Layer_Alter.xml"));
                LayerAlteringDictionary.Reload(xmlalterprovider, xlalterprovider);
            }
            if ((reload & ToReload.Legend) == ToReload.Legend)
            {
                ExcelStructDictionaryDataProvider<string, LegendData> xllegendprovider = new ExcelStructDictionaryDataProvider<string, LegendData>(PathProvider.GetPath("Layer_Props.xlsm"), "Legend");
                XmlDictionaryDataProvider<string, LegendData> xmllegendprovider = new XmlDictionaryDataProvider<string, LegendData>(PathProvider.GetPath("Layer_Legend.xml"));
                LayerLegendDictionary.Reload(xmllegendprovider, xllegendprovider);
            }
            if ((reload & ToReload.LegendDraw) == ToReload.LegendDraw)
            {
                ExcelStructDictionaryDataProvider<string, LegendDrawTemplate> xllegenddrawprovider = new ExcelStructDictionaryDataProvider<string, LegendDrawTemplate>(PathProvider.GetPath("Layer_Props.xlsm"), "LegendDraw");
                XmlDictionaryDataProvider<string, LegendDrawTemplate> xmllegenddrawprovider = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathProvider.GetPath("Layer_LegendDraw.xml"));
                LayerLegendDrawDictionary.Reload(xmllegenddrawprovider, xllegenddrawprovider);
            }
        }

        /// <summary>
        /// Выгрузка слоёв чертежа в Excel
        /// </summary>
        [CommandMethod("EXTRACTLAYERS")]
        public static void ExtractLayersInfoToExcel()
        {
            Workstation.Define();
            HostMgd.ApplicationServices.Document doc = HostMgd.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            TransactionManager tm = HostApplicationServices.WorkingDatabase.TransactionManager;
            Transaction transaction = tm.StartTransaction();
            using (transaction)
            {
                LayerTable lt = tm.TopTransaction.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layers = (from ObjectId elem in lt
                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForRead)
                              select ltr).ToList();
                //int i = 1;
                //try
                //{
                List<LayerProps> props = new List<LayerProps>();
                foreach (LayerTableRecord ltr in layers)
                {
                    string checkedname = "";
                    LayerProps lp = new LayerProps();
                    // Попытка распарсить имя слоя для поиска существующих сохранённых свойств
                    try
                    {
                        checkedname = new SimpleLayerParser(ltr.Name).LayerInfo.TrueName;
                    }
                    catch (WrongLayerException ex)
                    {
                        doc.Editor.WriteMessage(ex.Message);
                    }
                    bool lpsuccess = true;
                    try
                    {
                        lp = LayerPropertiesDictionary.GetValue(checkedname, out lpsuccess, false);
                    }
                    catch (NoPropertiesException)
                    {
                        lpsuccess = false;
                    }
                    props.Add(lp);


                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 1]).Value = checkedname != "" ? checkedname : ltr.Name;
                    //if (lpsuccess)
                    //{
                    //    ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 2]).Value = lp.ConstantWidth;
                    //    ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 3]).Value = lp.LTScale;
                    //}
                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 4]).Value = (int)ltr.Color.Red;
                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 5]).Value = (int)ltr.Color.Green;
                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 6]).Value = (int)ltr.Color.Blue;
                    //LinetypeTableRecord lttr = transaction.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 7]).Value = lttr.Name;
                    //((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 8]).Value = ltr.LineWeight;
                    //i++;
                    //    }
                    //}
                    //finally
                    //{
                    //workbook.SaveAs("ExtractedLayers.xlsx");
                    //workbook.Close();
                    //xlapp.Quit();
                    //}
                }
            }
        }
    }
}