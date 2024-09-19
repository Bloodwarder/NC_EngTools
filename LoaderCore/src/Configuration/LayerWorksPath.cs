using Microsoft.Extensions.Configuration;

namespace LoaderCore.Configuration
{
    public class LayerWorksPath
    {
        public PathRoute Route { get; set; }
        public readonly string? Path;
    }
}
