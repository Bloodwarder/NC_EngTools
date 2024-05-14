using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("FilterSection")]
    public class LegendFilterSection
    {
        public LegendFilterSection() { }

        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlArrayItem("Filter")]
        public LegendFilter[] Filters { get; set; }
    }
}
