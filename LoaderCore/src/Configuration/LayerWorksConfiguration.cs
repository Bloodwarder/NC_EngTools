using System.Collections.Generic;

namespace LoaderCore.Configuration
{
    public class LayerWorksConfiguration
    {
        public static List<LayerWorksPath> NameParserPaths;
        public static List<LayerWorksPath> LayerStandardPaths;
        public LegendGridParameters? LegendGridParameters { get; set; }
    }
}
