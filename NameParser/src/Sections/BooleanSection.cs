using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Суффикс, наличие которого определяет bool значение. Необязательный. Независим от положения.
    /// Может быть несколько (разного вида). Не считается частью основного имени
    /// </summary>
    public class BooleanSection : NamedParserSection
    {
        private string _suffix { get; init; }
        private string _trueDescription { get; init; }
        private string _falseDescription { get; init; }


        public BooleanSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute valueAttr = xElement.Attribute("Value") ?? throw new NameParserInitializeException("Ошибка инициализации суффикса. Отсутствует значение");
            XAttribute descriptionAttr = xElement.Attribute("Description") ?? throw new NameParserInitializeException("Ошибка инициализации суффикса. Отсутствует описание");
            XAttribute? trueDescriptionAttr = xElement.Attribute("TrueDescription");
            XAttribute? falseDescriptionAttr = xElement.Attribute("FalseDescription");
            _suffix = valueAttr.Value;
            Description = descriptionAttr.Value;
            _trueDescription = trueDescriptionAttr?.Value ?? "Отмеченные";
            _falseDescription = falseDescriptionAttr?.Value ?? "Обычные";
            parentParser.Suffixes.Add(Name, this);
        }

        internal string Description { get; init; }


        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (pointer > str.Length - 1)
            {
                layerInfo.SuffixTagged[Name] = false;
                return;
            }
            if (str[pointer] == _suffix)
            {
                layerInfo.SuffixTagged[Name] = true;
                pointer++;
            }
            else
            {
                layerInfo.SuffixTagged[Name] = false;
            }
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            // Суффикс только для полного имени
            bool success = layerInfo.SuffixTagged.TryGetValue(Name, out bool tagged);
            if (!success)
                layerInfo.SuffixTagged[Name] = false;
            if (nameType == NameType.FullName && tagged)
                inputList.Add(_suffix);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return str == _suffix;
        }
        internal override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptionFunc)
        {
            IEnumerable<bool> values = layerInfos.Select(x => x.SuffixTagged[Name]).Distinct();
            keywords = values.Select(b => b ? _trueDescription : _falseDescription).ToArray();
            descriptionFunc = s => s;
        }

        internal override void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions)
        {
            keywords = new[] { Name };
            descriptions = s => _trueDescription;

        }
    }
}
