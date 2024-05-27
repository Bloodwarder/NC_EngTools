using NameClassifiers.References;
using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("StatusGroup")]
    public class StatusGroup : ReferenceCollectionFilter
    {
        public StatusGroup() { }
        
        [XmlElement(ElementName ="DefaultValue", Type= typeof(ValueContainer))]
        public ValueContainer? DefaultValue;
    }
}
