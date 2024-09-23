using System.Xml.Serialization;

namespace NameClassifiers.Highlighting
{
    [XmlRoot("Property")]
    public class SinglePropertyHandler
    {
#nullable disable warnings
        public SinglePropertyHandler() { }
#nullable restore warnings
        [XmlAttribute(AttributeName = "Name")]
        public string PropertyName { get; set; }

        [XmlElement(ElementName = "Assign", Type = typeof(FilteredPropertyAssigner))]
        public FilteredPropertyAssigner[] FilteredPropertyAssigners { get; set; }

    }



}
