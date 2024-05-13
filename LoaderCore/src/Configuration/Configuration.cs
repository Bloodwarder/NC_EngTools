using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration.Xml;
using Microsoft.Extensions.Configuration;
using System.IO;
using LoaderCore.Utilities;

namespace LoaderCore.Configuration
{
    public static class Configuration
    {
        private const string ConfigurationFileName = "Configuration.xml";
        private static readonly IConfiguration _config = null!;

        static Configuration()
        {
            string xmlPath = PathProvider.GetPath(ConfigurationFileName);
            IConfiguration config = new ConfigurationBuilder().AddXmlFile(xmlPath).Build();
            _config = config;
        }

        public static Directories Directories => _config.GetRequiredSection(nameof(Directories)).Get<Directories>();
        public static LayerWorksConfiguration LayerWorks => _config.GetSection(nameof(LayerWorks)).Get<LayerWorksConfiguration>();
        public static GeoModConfiguration GeoMod => _config.GetSection(nameof(GeoMod)).Get<GeoModConfiguration>();
        public static UtilitiesConfiguration Utilities => _config.GetSection(nameof(Utilities)).Get<UtilitiesConfiguration>();

    }

    public class Directories
    {
        internal string LocalDirectory { get; set; }
        internal string UpdateDirectory { get; set; }
    }

    public class LayerWorksConfiguration
    {
        List<LayerWorksPath> NameParserPaths;
        List<LayerWorksPath> LayerStandardPaths;
        public LegendGridParameters? LegendGridParameters { get; set; }
    }

    public class LegendGridParameters
    {
        public double CellWidth { get; set; }
        public double CellHeight { get; set; }
        public double WidthInterval { get; set; }
        public double HeightInterval { get; set; }
        public double TextWidth { get; set; }
        public double TextHeight { get; set; }
    }

    public class LayerWorksPath
    {
        readonly PathRoute Route;
        readonly string? Path;
    }

    public enum PathRoute
    {
        Shared = 1,
        Local = 2,
        Overrides = 3
    }
    public class GeoModConfiguration
    {

    }

    public class UtilitiesConfiguration
    {
        public double DefaultLabelTextHeight { get; set; }
        public double DefaultLabelBackgroundScaleFactor { get; set; }
        public Vertical Vertical { get; set; }

    }

    public class Vertical
    {
        public string BlackMarkTag { get; set; }
        public string RedMarkTag { get; set; }
        public string SlopeTag { get; set; }
        public string DistanceTag { get; set; }
        public string ElevationMarkBlockName { get; set; }
        public string SlopeBlockName { get; set; }
        public double LastHorStep { get; set; }
    }
}
