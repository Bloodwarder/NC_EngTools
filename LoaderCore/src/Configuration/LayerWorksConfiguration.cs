using System.Collections.Generic;

namespace LoaderCore.Configuration
{
    public class LayerWorksConfiguration
    {
        List<LayerWorksPath> NameParserPaths;
        List<LayerWorksPath> LayerStandardPaths;
        public LegendGridParameters? LegendGridParameters { get; set; }
    }
}
