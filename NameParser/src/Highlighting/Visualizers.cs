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
