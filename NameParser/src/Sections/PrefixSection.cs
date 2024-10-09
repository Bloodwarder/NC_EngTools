using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Префикс. Обязательный. Всегда первый. Только один. Не считается частью основного имени
    /// </summary>
    public class PrefixSection : ParserSection
    {
        public PrefixSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute xAttribute = xElement.Attribute("Value") ?? throw new NameParserInitializeException("Ошибка инициализации префикса. Отсутствует значение");
            string prefix = xAttribute.Value;
            if (NameParser.LoadedParsers.ContainsKey(prefix))
                throw new NameParserInitializeException($"Ошибка инициализации префикса. Префикс {prefix} уже загружен");
            parentParser.Prefix = prefix;
        }

        internal override void Process(string[] str, LayerInfoResult layerInfoResult, int pointer)
        {
            // Проверяем совпадение префикса (вообще по идее сюда попасть не должно)
            if (str[pointer] != layerInfoResult.Value.ParentParser.Prefix)
                throw new WrongLayerException("Не совпадает префикс");
            // Назначаем префикс
            layerInfoResult.Value.Prefix = layerInfoResult.Value.ParentParser.Prefix;
            pointer++;
            NextSection?.Process(str, layerInfoResult, pointer);
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

        internal override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions)
        {
            throw new NotImplementedException("Префикс всегда один");
        }

        internal override void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions)
        {
            throw new NotImplementedException();
        }
    }
}
