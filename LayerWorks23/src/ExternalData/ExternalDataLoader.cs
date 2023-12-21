using Microsoft.Office.Interop.Excel;
using System.Linq;
using Teigha.Runtime;
using Teigha.DatabaseServices;
using LoaderCore.Utilities;
using LayerWorks.ModelspaceDraw;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using Excel = Microsoft.Office.Interop.Excel;

namespace LayerWorks.ExternalData
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
                ExcelStructDictionaryDataProvider<string, LayerProps> xlpropsprovider = new ExcelStructDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Excel"), "Props");
                XmlDictionaryDataProvider<string, LayerProps> xmlpropsprovider = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props"));
                LayerPropertiesDictionary.Reload(xmlpropsprovider, xlpropsprovider);
            }
            if ((reload & ToReload.Alter) == ToReload.Alter)
            {
                ExcelSimpleDictionaryDataProvider<string, string> xlalterprovider = new ExcelSimpleDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Excel"), "Alter");
                XmlDictionaryDataProvider<string, string> xmlalterprovider = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter"));
                LayerAlteringDictionary.Reload(xmlalterprovider, xlalterprovider);
            }
            if ((reload & ToReload.Legend) == ToReload.Legend)
            {
                ExcelStructDictionaryDataProvider<string, LegendData> xllegendprovider = new ExcelStructDictionaryDataProvider<string, LegendData>(PathOrganizer.GetPath("Excel"), "Legend");
                XmlDictionaryDataProvider<string, LegendData> xmllegendprovider = new XmlDictionaryDataProvider<string, LegendData>(PathOrganizer.GetPath("Legend"));
                LayerLegendDictionary.Reload(xmllegendprovider, xllegendprovider);
            }
            if ((reload & ToReload.LegendDraw) == ToReload.LegendDraw)
            {
                ExcelStructDictionaryDataProvider<string, LegendDrawTemplate> xllegenddrawprovider = new ExcelStructDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("Excel"), "LegendDraw");
                XmlDictionaryDataProvider<string, LegendDrawTemplate> xmllegenddrawprovider = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw"));
                LayerLegendDrawDictionary.Reload(xmllegenddrawprovider, xllegenddrawprovider);
            }
        }

        /// <summary>
        /// Выгрузка слоёв чертежа в Excel
        /// </summary>
        [CommandMethod("EXTRACTLAYERS")]
        public static void ExtractLayersInfoToExcel()
        {
            Application xlapp = new Application
            { DisplayAlerts = false };
            Workbook workbook = xlapp.Workbooks.Add();
            HostMgd.ApplicationServices.Document doc = HostMgd.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            TransactionManager tm = HostApplicationServices.WorkingDatabase.TransactionManager;
            Transaction transaction = tm.StartTransaction();
            using (transaction)
            {
                LayerTable lt = tm.TopTransaction.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layers = (from ObjectId elem in lt
                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForRead)
                              select ltr).ToList();
                int i = 1;
                try
                {
                    foreach (LayerTableRecord ltr in layers)
                    {
                        string checkedname = "";
                        LayerProps lp = new LayerProps();
                        // Попытка распарсить имя слоя для поиска существующих сохранённых свойств
                        try
                        {
                            checkedname = new SimpleLayerParser(ltr.Name).TrueName;
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

                        //bool lpsuccess = LayerProperties._dictionary.TryGetValue(checkedname, out LayerProps lp);
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 1]).Value = checkedname != "" ? checkedname : ltr.Name;
                        if (lpsuccess)
                        {
                            ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 2]).Value = lp.ConstantWidth;
                            ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 3]).Value = lp.LTScale;
                        }
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 4]).Value = (int)ltr.Color.Red;
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 5]).Value = (int)ltr.Color.Green;
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 6]).Value = (int)ltr.Color.Blue;
                        LinetypeTableRecord lttr = transaction.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 7]).Value = lttr.Name;
                        ((Excel.Range)((Excel.Range)workbook.Worksheets[1]).Cells[i, 8]).Value = ltr.LineWeight;
                        i++;
                    }
                }
                finally
                {
                    workbook.SaveAs(PathOrganizer.BasePath + "ExtractedLayers.xlsx");
                    workbook.Close();
                    xlapp.Quit();
                }
            }
        }
    }
}