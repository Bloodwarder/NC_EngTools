using System.Xml.Linq;

namespace NameClassifiers.Sections
{

    internal class BooleanSection : ParserSection
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
            if (str[pointer] == _suffix)
            {
                layerInfo.SuffixTagged = true;
                pointer++;
            }
            else
            {
                layerInfo.SuffixTagged = false;
            }
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {
            if (layerInfo.SuffixTagged)
                inputList.Add(_suffix);
            NextSection?.ComposeName(inputList, layerInfo);
        }
        internal override bool ValidateString(string str)
        {
            return str==_suffix;
        }
    }
}
