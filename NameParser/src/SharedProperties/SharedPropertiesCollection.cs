using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("SharedProperties")]
    public class SharedPropertiesCollection //: ICollection<SharedProperty>
    {
#nullable disable warnings
        public SharedPropertiesCollection() { }
#nullable restore

        [XmlElement(ElementName = "Property", Type = typeof(SharedProperty))]
        public SharedProperty[] Properties { get; set; }
    }
}
