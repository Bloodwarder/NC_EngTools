using System.Xml.Serialization;

namespace NameClassifiers.Highlighting
{
    [XmlRoot("Filter")]
    public class HighlightFilter
    {
        public HighlightFilter() { }

        [XmlAttribute(AttributeName = "Name", Type = typeof(string))]
        public string Name { get; set; } = null!;

        [XmlElement(ElementName = "Highlight", Type = typeof(FilterModeHandler))]
        public FilterModeHandler? Highlight { get; set; }

        [XmlElement(ElementName = "Enable", Type = typeof(FilterModeHandler))]
        public FilterModeHandler? Enable { get; set; }

        [XmlElement(ElementName = "Disable", Type = typeof(FilterModeHandler))]
        public FilterModeHandler? Disable { get; set; }
    }



}
