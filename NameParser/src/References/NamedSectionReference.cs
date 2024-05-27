using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlInclude(typeof(ClassifierReference))]
    [XmlInclude(typeof(DataReference))]
    [XmlInclude(typeof(BoolReference))]
    public abstract class NamedSectionReference : SectionReference
    {
        [XmlAttribute("Name")]
        public string Name { get; set; } = null!;
    }
}