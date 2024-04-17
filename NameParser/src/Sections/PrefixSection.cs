using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    internal class PrefixSection : ParserSection
    {
        public PrefixSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute xAttribute = xElement.Attribute("Value") ?? throw new NameParserInitializeException("Ошибка инициализации префикса. Отсутствует значение");
            parentParser.Prefix = xAttribute.Value;
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Проверяем совпадение префикса (вообще по идее сюда попасть не должно)
            if (str[pointer] != layerInfo.ParentParser.Prefix)
                throw new WrongLayerException("Не совпадает префикс");
            // Назначаем префикс
            layerInfo.Prefix = layerInfo.ParentParser.Prefix;
            pointer++;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {
            inputList.Add(ParentParser.Prefix);
            NextSection?.ComposeName(inputList, layerInfo);
        }
        internal override bool ValidateString(string str)
        {
            return str == ParentParser.Prefix;
        }
    }
}
