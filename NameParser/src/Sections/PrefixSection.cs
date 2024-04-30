using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    internal class PrefixSection : ParserSection
    {
        public PrefixSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute xAttribute = xElement.Attribute("Value") ?? throw new NameParserInitializeException("Ошибка инициализации префикса. Отсутствует значение");
            string prefix = xAttribute.Value;
            if (NameParser.LoadedParsers.ContainsKey(prefix))
                throw new NameParserInitializeException($"Ошибка инициализации префикса. Префикс {prefix} уже загружен");
            parentParser.Prefix = prefix;
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
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            // Префикс добавляется только в полное имя
            if (nameType == NameType.FullName)
                inputList.Add(ParentParser.Prefix);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return str == ParentParser.Prefix;
        }
    }
}
