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
        private string _description { get; init; }

        public BooleanSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute valueAttr = xElement.Attribute("Value") ?? throw new NameParserInitializeException("Ошибка инициализации суффикса. Отсутствует значение");
            XAttribute descriptionAttr = xElement.Attribute("Description") ?? throw new NameParserInitializeException("Ошибка инициализации суффикса. Отсутствует описание");
            _suffix = valueAttr.Value;
            _description = descriptionAttr.Value;
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (pointer > str.Length - 1)
                return;
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
    }
}
