using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    internal abstract class ParserSection
    {
        protected NameParser ParentParser { get; init; }
        protected ParserSection(XElement xElement, NameParser parentParser) 
        { 
            this.ParentParser = parentParser;
        }

        internal ParserSection? NextSection { get; set; }

        internal abstract void Process(string[] str, LayerInfo layerInfo, int pointer);
        internal abstract void ComposeName(List<string> inputList, LayerInfo layerInfo);
        internal abstract bool ValidateString(string str);
    }
}
