using LayersIO.DataTransfer;
using LayersIO.Excel;
using LayersIO.Xml;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using NanocadUtilities;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using static LoaderCore.NcetCore;

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
        //[CommandMethod("RELOADPROPS")]
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
                ExcelComplexLayerDataProvider<string, LayerProps> xlpropsprovider = new(PathProvider.GetPath("Layer_Props.xlsm"), "Props");
                XmlLayerDataWriter<string, LayerProps> xmlpropsprovider = new(PathProvider.GetPath("Layer_Props.xml"));
                NcetCore.ServiceProvider.GetService<LayerPropertiesDictionary>().Reload(xmlpropsprovider, xlpropsprovider);
            }
            if ((reload & ToReload.Alter) == ToReload.Alter)
            {
                ExcelSimpleLayerDataProvider<string, string> xlalterprovider = new(PathProvider.GetPath("Layer_Props.xlsm"), "Alter");
                XmlLayerDataWriter<string, string> xmlalterprovider = new(PathProvider.GetPath("Layer_Alter.xml"));
                NcetCore.ServiceProvider.GetService<LayerAlteringDictionary>().Reload(xmlalterprovider, xlalterprovider);
            }
            if ((reload & ToReload.Legend) == ToReload.Legend)
            {
                ExcelComplexLayerDataProvider<string, LegendData> xllegendprovider = new(PathProvider.GetPath("Layer_Props.xlsm"), "Legend");
                XmlLayerDataWriter<string, LegendData> xmllegendprovider = new(PathProvider.GetPath("Layer_Legend.xml"));
                NcetCore.ServiceProvider.GetService<LayerLegendDictionary>().Reload(xmllegendprovider, xllegendprovider);
            }
            if ((reload & ToReload.LegendDraw) == ToReload.LegendDraw)
            {
                ExcelComplexLayerDataProvider<string, LegendDrawTemplate> xllegenddrawprovider = new(PathProvider.GetPath("Layer_Props.xlsm"), "LegendDraw");
                XmlLayerDataWriter<string, LegendDrawTemplate> xmllegenddrawprovider = new(PathProvider.GetPath("Layer_LegendDraw.xml"));
                NcetCore.ServiceProvider.GetService<LayerLegendDrawDictionary>().Reload(xmllegenddrawprovider, xllegenddrawprovider);
            }
        }

        /// <summary>
        /// Выгрузка слоёв чертежа в Excel.
        /// </summary>
        //[CommandMethod("EXTRACTLAYERS")]
        public static void ExtractLayersInfoToExcel()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                if (LayerWrapper.StandartPrefix == null)
                {
                    Workstation.Editor.WriteMessage("Отсутствует префикс для выборки слоёв");
                    return;
                }
                LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layers = (from ObjectId elem in lt!
                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForRead)
                              where ltr.Name.StartsWith(LayerWrapper.StandartPrefix)
                              select ltr).ToList();
                //int i = 1;
                //try
                //{
                List<LayerProps> props = new();
                foreach (LayerTableRecord ltr in layers)
                {
                    string checkedname = "";

                    // Попытка распарсить имя слоя для поиска существующих сохранённых свойств
                    LayerInfoResult checkinfo = LayerWrapper.GetInfoFromString(ltr.Name);
                    if (checkinfo.Status == LayerInfoParseStatus.Success)
                    {
                        checkedname = checkinfo.Value.TrueName;
                    }
                    else
                    {
                        var exception = checkinfo.GetExceptions().First();
                        Logger?.LogWarning(exception, "{Message}", exception.Message);
                    }

                    var service = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
                    bool lpsuccess = service.TryGet(checkedname, out var lp);
                    if (lpsuccess)
                        props.Add(lp!);

                    throw new NotImplementedException();
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