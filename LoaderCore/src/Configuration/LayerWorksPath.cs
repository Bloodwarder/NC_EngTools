using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LoaderCore.Configuration
{
    [XmlRoot(ElementName = nameof(LayerWorksPath))]
    public class LayerWorksPath
    {
        [NonSerialized]
        private DirectoryInfo? _directoryInfo;

        public DirectoryInfo? DirectoryInfo
        {
            get
            {
                if (_directoryInfo == null && Path != null)
                    _directoryInfo = new DirectoryInfo(Path);
                return _directoryInfo;
            }
        }
        [XmlElement(ElementName = nameof(Type))]
        public PathRoute? Type { get; set; }

        [XmlElement(ElementName = nameof(Path))]
        public string? Path { get; set; }
    }
}
