using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    public abstract class NamedParserSection : ParserSection
    {
        protected NamedParserSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute nameAttr = xElement.Attribute("Name") ?? throw new NameParserInitializeException("Отсутствует необходимое имя секции");
            Name = nameAttr.Value;
        }
        internal string Name { get; init; }

    }
}
