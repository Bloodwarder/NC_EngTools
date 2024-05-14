using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(DataReference))]
    public class DataReference : SectionReference
    {
        [XmlAttribute("Name")]
        public string? Name { get; set; }
    }
}