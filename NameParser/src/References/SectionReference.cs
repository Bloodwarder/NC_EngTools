using System.Xml.Serialization;


namespace NameClassifiers.References
{

    [XmlInclude(typeof(ChapterReference))]
    [XmlInclude(typeof(StatusReference))]
    [XmlInclude(typeof(ClassifierReference))]
    [XmlInclude(typeof(DataReference))]
    public abstract class SectionReference
    {
        [XmlAttribute("Value")]
        public string? Value { get; set; }
    }
}