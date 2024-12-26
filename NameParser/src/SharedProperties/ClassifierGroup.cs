using NameClassifiers.References;
using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("ClassifierGroup")]
    public class ClassifierGroup : ReferenceCollectionFilter
    {
        public ClassifierGroup() { }

        [XmlElement(ElementName = "DefaultValue", Type = typeof(ValueContainer))]
        public ValueContainer? DefaultValue;
    }
}
