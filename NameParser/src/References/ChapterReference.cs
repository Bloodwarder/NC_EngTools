using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(ChapterReference))]
    public class ChapterReference : SectionReference { }
}