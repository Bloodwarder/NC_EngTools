using NameClassifiers.Filters;
using System.Collections;
using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("SharedProperties")]
    public class SharedProperties //: ICollection<SharedProperty>
    {
        public SharedProperties() { }

        [XmlElement(ElementName = "Property", Type = typeof(SharedProperty))]
        public SharedProperty[] Properties { get; set; }
    }
}
