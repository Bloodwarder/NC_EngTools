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
            var objs = mapper.Take<LayerPropertiesData>("Props");

            

        }
    }
}
