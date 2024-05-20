using System.Xml.Serialization;

namespace NameClassifiers.References
{
    [XmlRoot(ElementName = nameof(BoolReference))]

    internal class BoolReference : SectionReference
    {
        [XmlAttribute(AttributeName ="Name",Type =typeof(string))]
        public string? Name { get; set; }
        public override bool Match(LayerInfo layerInfo) => layerInfo.SuffixTagged;
    }
}

