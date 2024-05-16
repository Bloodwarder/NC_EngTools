using NameClassifiers.References;
using System.Xml.Serialization;


namespace NameClassifiers.SharedProperties
{
    [XmlRoot("StatusGroup")]
    public class StatusGroup
    {
        public StatusGroup() { }
        
        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        public SectionReference[] References { get; set; }
        [XmlElement(ElementName ="DefaultValue", Type= typeof(ValueContainer))]
        public ValueContainer? DefaultValue;
    }
}
