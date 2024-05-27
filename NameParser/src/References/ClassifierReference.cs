using System.Xml.Serialization;


namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(ClassifierReference))]
    public class ClassifierReference : NamedSectionReference
    {
        ///<inheritdoc/>
        public override bool Match(LayerInfo layerInfo) => layerInfo.AuxilaryClassifiers[Name] == Value;
    }
}