using LayersIO.Connection;
using LayersIO.Model;
using LoaderCore.Utilities;
using Microsoft.EntityFrameworkCore;
using Npoi.Mapper;
using System.Diagnostics;

namespace LayersIO.Excel
{
    public class ExcelLayerReader
    {
        public static void ReadWorkbook(string workbookPath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Mapper mapper = new(workbookPath);

            List<LayerGroupData> groups = ExtractLayerGroupsData(mapper);
            List<LayerData> layers = ExtractLayersData(mapper, groups);
            using (TestLayersDatabaseContextSqlite db = new("C:\\Users\\konovalove\\source\\repos\\Bloodwarder\\NC_EngTools\\NC_EngTools\\bin\\Debug\\testdb.db"))
            {
                db.LayerGroupData.AddRange(groups);
                db.LayerData.AddRange(layers);
                db.SaveChanges();
                //ExportEntities(groups, db, lg => lg.MainName);
                //ExportEntities(layers, db, lg => lg.Name);
                //Logger.WriteLog($"Время операции - {Math.Round(sw.Elapsed.TotalSeconds, 2)}");
            }
            Logger.WriteLog($"Операция завершена. Время операции - {Math.Round(sw.Elapsed.TotalSeconds, 2)}");
        }

        public async static Task<string> ReadWorkbookAsync(string workbookPath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            await Task.Run(() =>
            {
                Mapper mapper = new(workbookPath);

                List<LayerGroupData> groups = ExtractLayerGroupsData(mapper);
                List<LayerData> layers = ExtractLayersData(mapper, groups);
                using (TestLayersDatabaseContextSqlite db = new("C:\\Users\\konovalove\\source\\repos\\Bloodwarder\\NC_EngTools\\NC_EngTools\\bin\\Debug\\testdb.db"))
                {
                    db.LayerGroupData.AddRange(groups);
                    db.LayerData.AddRange(layers);
                    db.SaveChangesAsync();
                    //ExportEntities(groups, db, lg => lg.MainName);
                    //ExportEntities(layers, db, lg => lg.Name);
                    //Logger.WriteLog($"Время операции - {Math.Round(sw.Elapsed.TotalSeconds, 2)}");
                }
            });
            return $"Операция импорта слоёв из Excel завершена. Время операции - {Math.Round(sw.Elapsed.TotalSeconds, 2)}";
        }

        private static List<LayerGroupData> ExtractLayerGroupsData(Mapper mapper)
        {
            Dictionary<string, LayerLegendData> legendDataDictionary =
                ExtractToDictionary<LayerLegendData, NameTransition>(mapper, "Legend", n => n.Value.MainName!);
            var alterDictionary = mapper.Take<NameTransition>("Alter").ToDictionary(n => n.Value.MainNameSource!, n => n.Value.MainNameAlter);
            List<LayerGroupData> list = new();
            foreach (var layerGroup in legendDataDictionary.Keys)
            {
                LayerGroupData layerGroupData = new()
                {
                    MainName = layerGroup,
                    LayerLegendData = legendDataDictionary[layerGroup]
                };
                list.Add(layerGroupData);
            }
            foreach (var layerGroup in list)
            {
                if (alterDictionary.ContainsKey(layerGroup.MainName))
                    layerGroup.AlternateLayer = alterDictionary[layerGroup.MainName];
            }
            return list;
        }
        private static List<LayerData> ExtractLayersData(Mapper mapper, IEnumerable<LayerGroupData> layerGroups)
        {
            Dictionary<string, LayerPropertiesData> propsDictionary =
                ExtractToDictionary<LayerPropertiesData, NameTransition>(mapper, "Props", n => n.Value.TrueName!);
            Dictionary<string, LayerDrawTemplateData> legendDrawDictionary =
                ExtractToDictionary<LayerDrawTemplateData, NameTransition>(mapper, "LegendDraw", n => n.Value.TrueName!);

            Dictionary<string, LayerGroupData> groupDictionary = layerGroups.ToDictionary(lg => lg.MainName, lg => lg);

            List<LayerData> layers = new();
            List<string> layerNames = propsDictionary.Select(n => n.Key).Union(legendDrawDictionary.Select(n => n.Key)).ToList();
            foreach (var layer in layerNames)
            {
                LayerData layerData = new()
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
                bool success = groupDictionary.TryGetValue(layerData.MainName, out LayerGroupData? lgd);
                if (success)
                    lgd!.Layers.Add(layerData);
                layers.Add(layerData);
            }
            return layers;
        }

        private static Dictionary<string, T> ExtractToDictionary<T, N>(Mapper mapper, string sheetname, Func<RowInfo<N>, string> nameExpression)
            where T : class
            where N : class
        {
            var mappedNames = mapper.Take<N>(sheetname).Select(nameExpression).ToList();
            var mappedData = mapper.Take<T>(sheetname).ToList();
            Dictionary<string, T> mappedDataDictionary = new();
            for (int i = 0; i < mappedNames.Count; i++)
            {
                mappedDataDictionary[mappedNames[i]!] = mappedData[i].Value;
            }
            return mappedDataDictionary;
        }

        private static void ExportEntities<T>(IEnumerable<T> entities, DbContext db, Func<T, string> entityNamesExpression) where T : class
        {
            int successCounter = 0;
            int failureCounter = 0;
            foreach (var entity in entities)
            {
                try
                {
                    db.Attach(entity);
                    db.SaveChanges();
                    successCounter++;
                }
                catch (DbUpdateException ex)
                {
                    Logger.WriteLog($"Ошибка при экспорте слоя {entityNamesExpression}");
                    Logger.WriteLog(ex.InnerException!.Message);
                    failureCounter++;
                    continue;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Ошибка при экспорте слоя {entityNamesExpression}.\n{ex.Message}");
                    continue;
                }
            }
            Logger.WriteLog($"Импорт объектов {typeof(T).Name} завершён. Успешно импортированно {successCounter} объектов. Ошибок - {failureCounter}");
        }


        class NameTransition
        {
            public string? MainName { get; set; }
            public string? TrueName { get; set; }
            public string? MainNameSource { get; set; }
            public string? MainNameAlter { get; set; }
        }
    }
}
