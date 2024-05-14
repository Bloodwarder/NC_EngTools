using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(StatusReference))]
    public class StatusReference : SectionReference { }
}