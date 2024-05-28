using NameClassifiers.Sections;
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

        public sealed override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions)
        {
            ParserSection section = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].GetSection(this.GetType(), Name);
            section.ExtractDistinctInfo(layerInfos, out keywords, out descriptions);
        }
    }
}