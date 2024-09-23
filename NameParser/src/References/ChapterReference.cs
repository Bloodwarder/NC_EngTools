using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(ChapterReference))]
    public class ChapterReference : SectionReference
    {
        public override bool Match(LayerInfo layerInfo) => layerInfo.PrimaryClassifier == Value;
    }
}