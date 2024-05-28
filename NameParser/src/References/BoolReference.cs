using System.Xml.Serialization;

namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(BoolReference))]
    public class BoolReference : NamedSectionReference
    {
#nullable disable warnings
        public BoolReference() : base() { }
#nullable restore warnings

        public override bool Match(LayerInfo layerInfo) => layerInfo.SuffixTagged[Name];
    }
}

