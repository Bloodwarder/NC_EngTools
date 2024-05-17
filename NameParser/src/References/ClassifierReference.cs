using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(ClassifierReference))]
    public class ClassifierReference : SectionReference
    {
        [XmlAttribute("Name")]
        public string Name { get; set; } = null!;

        public override bool Match(LayerInfo layerInfo) => layerInfo.AuxilaryClassifiers[Name] == Value;
    }
}