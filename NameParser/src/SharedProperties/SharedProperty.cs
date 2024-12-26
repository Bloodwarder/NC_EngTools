using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    //[XmlRoot("Property")]
    public class SharedProperty
    {
#nullable disable warnings
        public SharedProperty() { }
#nullable restore

        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "ClassifierGroup", Type = typeof(ClassifierGroup))]
        public ClassifierGroup[] Groups { get; set; }
    }
}
