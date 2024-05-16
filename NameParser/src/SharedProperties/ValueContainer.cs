using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    public class ValueContainer
    {
        public ValueContainer() { }
        [XmlElement(ElementName = "Double", Type = typeof(double))]
        [XmlElement(ElementName = "Color", Type = typeof(Color))]
        [XmlElement(ElementName = "String", Type = typeof(string))]
        public object? Value;
    }
}
