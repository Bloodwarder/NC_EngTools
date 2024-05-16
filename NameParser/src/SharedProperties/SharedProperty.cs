using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("Property")]
    public class SharedProperty
    {
        public SharedProperty() { }

        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "StatusGroup", Type = typeof(StatusGroup))]
        public StatusGroup[] Groups { get; set; }
    }
}
