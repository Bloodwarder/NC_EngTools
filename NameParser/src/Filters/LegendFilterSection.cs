using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("FilterSection")]
    public class LegendFilterSection
    {
        public LegendFilterSection() { }

        [XmlAttribute("Name")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "Filter", Type = typeof(LegendFilter))]
        public LegendFilter[] Filters { get; set; }
    }
}
