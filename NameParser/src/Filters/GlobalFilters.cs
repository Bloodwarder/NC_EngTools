using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("LegendFilters")]
    public class GlobalFilters
    {
        public GlobalFilters() { }
        [XmlAttribute("DefaultLabel")]
        public string? DefaultLabel { get; set; }

        [XmlElement(ElementName = "FilterSection", Type = typeof(LegendFilterSection))]
        public LegendFilterSection[]? Sections { get; set; }

    }

}
