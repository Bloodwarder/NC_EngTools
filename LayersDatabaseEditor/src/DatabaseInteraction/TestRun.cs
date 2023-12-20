using LayersDatabase.Model;
using LoaderCore.Utilities;
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


           
        }

        class NameTransition
        {
            public string? TrueName { get; set; }
            public string? MainNameSource { get; set; }
            public string? MainNameAlter { get; set; }
        }
    }
}
