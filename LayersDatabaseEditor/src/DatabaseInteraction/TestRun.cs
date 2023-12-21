using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using LoaderCore.Utilities;
using Nelibur.ObjectMapper;
using Npoi.Mapper;

namespace LayersDatabaseEditor.DatabaseInteraction
{
    internal class TestRun
    {
        public static void RunTest(DatabaseEditorWindow parentWindow)
        {
            Mapper mapper = new Mapper(PathProvider.GetPath("Layer_Props.xlsm"));
            var propsLayerNames = mapper.Take<NameTransition>("Props").Select(m => m.Value.TrueName).ToList();
            var props = mapper.Take<LayerPropertiesData>("Props").ToList();
            Dictionary<string, LayerPropertiesData> propsDictionary = new();
            for (int i = 0; i < propsLayerNames.Count; i++)
            {
                propsDictionary[propsLayerNames[i]!] = props[i].Value;
            }

            var legendDrawNames = mapper.Take<NameTransition>("LegendDraw").Select(m => m.Value?.TrueName).ToList();
            var legendDrawTemplates = mapper.Take<LayerDrawTemplateData>("LegendDraw").ToList();
            Dictionary<string, LayerDrawTemplateData> legendDrawDictionary = new();
            for (int i = 0; i < legendDrawNames.Count; i++)
            {
                legendDrawDictionary[legendDrawNames[i]!] = legendDrawTemplates[i].Value;
            }

            List<LayerData> layers = new List<LayerData>();
            List<string> layerNames = propsLayerNames.Union(legendDrawNames).ToList();
            foreach (var layer in propsLayerNames)
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
            using(TestLayersDatabaseContextSqlite db = new("Test.db"))
            {
                db.AddRange(layers);
                db.SaveChanges();
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
