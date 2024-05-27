using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NameClassifiers.Highlighting
{
    [XmlRoot("HighlightMode")]
    public class Visualizers
    {
#nullable disable warnings
        public Visualizers() { }
#nullable restore warnings
        [XmlElement(ElementName = "Filter", Type = typeof(HighlightFilter))]
        public HighlightFilter[] Filters { get; set; }
    }

}
