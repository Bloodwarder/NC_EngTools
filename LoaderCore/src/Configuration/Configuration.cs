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
    internal static class Configuration
    {
        private const string ConfigurationFileName = "Configuration.xml";

        static Configuration()
        {
            string xmlPath = PathProvider.GetPath(ConfigurationFileName);
            IConfiguration config = new ConfigurationBuilder().AddXmlFile(xmlPath).Build();
        }
    }

    internal class Directories
    {
        internal string LocalDirectory { get; set; }
        internal string UpdateDirectory { get; set; }
    }

    public class LayerWorksConfiguration
    {

    }

    public class GeoModConfiguration
    {

    }

    public class UtilitiesConfiguration
    {

    }
}
