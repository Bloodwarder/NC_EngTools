using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using LoaderCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Nelibur.ObjectMapper;
using Npoi.Mapper;
using System.Diagnostics;

namespace LayersIO.Excel
{
    public class ExcelLayerReader
    {
        public static void ReadWorkbook(string workbookPath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Mapper mapper = new Mapper(workbookPath);
            var propsLayerNames = mapper.Take<NameTransition>("Props").Select(m => m.Value.TrueName).ToList();
            var props = mapper.Take<LayerPropertiesData>("Props").ToList();
            Dictionary<string, LayerPropertiesData> propsDictionary = new();
            for (int i = 0; i < propsLayerNames.Count; i++)
            {
                propsDictionary[propsLayerNames[i]!] = props[i].Value;
            }

            var legendDrawNames = mapper.Take<NameTransition>("LegendDraw").Select(m => m.Value.TrueName).ToList();
            var legendDrawTemplates = mapper.Take<LayerDrawTemplateData>("LegendDraw").ToList();
            Dictionary<string, LayerDrawTemplateData> legendDrawDictionary = new();
            for (int i = 0; i < legendDrawNames.Count; i++)
            {
                legendDrawDictionary[legendDrawNames[i]!] = legendDrawTemplates[i].Value;
            }

            List<LayerData> layers = new List<LayerData>();
            List<string> layerNames = propsLayerNames.Union(legendDrawNames).ToList();
            foreach (var layer in layerNames)
            {
                LayerData layerData = new LayerData()
                {
                    Name = layer,
                };

                try
                {
                    layerData.LayerPropertiesData = propsDictionary[layer];
                }
                catch
                {
                    layerData.LayerPropertiesData = new();
                }
                try
                {
                    layerData.LayerDrawTemplateData = legendDrawDictionary[layer];
                }
                catch
                {
                    layerData.LayerDrawTemplateData = new();
                }
                layers.Add(layerData);
            }
            using (TestLayersDatabaseContextSqlite db = new("C:\\Users\\konovalove\\source\\repos\\Bloodwarder\\NC_EngTools\\NC_EngTools\\bin\\Debug\\testdb.db"))
            {
                int successCounter = 0;
                int failureCounter = 0;
                foreach (var layer in layers)
                {
                    try
                    {
                        db.LayerData.Add(layer);
                        db.SaveChanges();
                        successCounter++;
                    }
                    catch (DbUpdateException ex)
                    {
                        Logger.WriteLog($"Ошибка при экспорте слоя {layer.Name}");
                        Logger.WriteLog(ex.InnerException!.Message);
                        failureCounter++;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog($"Ошибка при экспорте слоя {layer.Name}.\n{ex.Message}");
                        continue;
                    }
                }
                Logger.WriteLog($"Импорт завершён. Успешно импортированно {successCounter} слоёв. Ошибок - {failureCounter}. Операция заняла {sw.Elapsed.TotalSeconds} секунд");
            }

        }

        class NameTransition
        {
            public string? TrueName { get; set; }
            public string? MainNameSource { get; set; }
            public string? MainNameAlter { get; set; }
        }
    }
}
