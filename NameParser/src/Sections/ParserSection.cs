using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Секция классификатора. Секции последовательно собираются в цепочку обязанностей 
    /// в конструкторе NameParser и последовательно обрабатывают входящие массивы строк или объекты LayerInfo.
    /// </summary>
    public abstract class ParserSection
    {
        protected NameParser ParentParser { get; init; }
        protected ParserSection(XElement xElement, NameParser parentParser) 
        { 
            this.ParentParser = parentParser;
        }

        internal ParserSection? NextSection { get; set; }

        internal abstract void Process(string[] str, LayerInfo layerInfo, int pointer);
        internal abstract void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType);
        internal abstract bool ValidateString(string str);
        internal abstract void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions);

    }
}
