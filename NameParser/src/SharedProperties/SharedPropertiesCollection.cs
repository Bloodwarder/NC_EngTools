using NameClassifiers.Filters;
using System.Collections;
using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("SharedProperties")]
    public class SharedPropertiesCollection //: ICollection<SharedProperty>
    {
        public SharedPropertiesCollection() { }

        [XmlElement(ElementName = "Property", Type = typeof(SharedProperty))]
        public SharedProperty[] Properties { get; set; }
    }
}
