using System.Xml.Serialization;


namespace NameClassifiers.References
{


    public abstract class SectionReference
    {
        [XmlAttribute("Value")]
        public string? Value { get; set; }
    }
}