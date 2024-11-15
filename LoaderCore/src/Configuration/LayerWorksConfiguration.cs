using System.Collections.Generic;

namespace LoaderCore.Configuration
{
    public class LayerWorksConfiguration
    {
#nullable disable warnings
        public LayerWorksConfiguration() { }
#nullable restore

        public List<LayerWorksPath> NameParserPaths;
        public List<LayerWorksPath> LayerStandardPaths;
        //public LegendGridParameters? LegendGridParameters { get; set; }
    }
}
