using System.Collections.Generic;

namespace LoaderCore.Configuration
{
    public class LayerWorksConfiguration
    {
        public List<LayerWorksPath> NameParserPaths;
        public List<LayerWorksPath> LayerStandardPaths;
        public LegendGridParameters? LegendGridParameters { get; set; }
    }
}
