using LoaderCore.Utilities;
using Microsoft.Extensions.Configuration;

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
}
