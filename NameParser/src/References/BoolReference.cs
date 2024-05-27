using System.Xml.Serialization;

namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(BoolReference))]
    public class BoolReference : NamedSectionReference
    {
        public override bool Match(LayerInfo layerInfo) => layerInfo.SuffixTagged[Name];
    }
}

