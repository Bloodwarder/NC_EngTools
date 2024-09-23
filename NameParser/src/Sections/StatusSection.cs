using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Статус. Обязательный. Независим от положения. Может быть только один.
    /// Считается частью основного имени TrueName (но не группового имени MainName)
    /// </summary>
    public class StatusSection : ParserSection
    {
        private readonly Dictionary<string, string> _descriptionDict = new();
        internal StatusSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            foreach (XElement chapter in xElement.Elements("Status"))
            {
                XAttribute? keyAttr = chapter.Attribute("Value");
                XAttribute? descriptionAttr = chapter.Attribute("Description");

                if (keyAttr != null && descriptionAttr != null)
                    _descriptionDict.Add(keyAttr.Value, descriptionAttr.Value);
                else
                    throw new NameParserInitializeException("Ошибка инициализации классификатора статуса. Неверный ключ или описание");
            }
        }

        internal Dictionary<string, string> GetDescriptionDictionary() => _descriptionDict;
        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (!_descriptionDict.ContainsKey(str[pointer]))
                throw new WrongLayerException("Не найден статус");
            layerInfo.Status = str[pointer];
            pointer++;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            // Статус добавляется в любое имя кроме MainName
            if (nameType != NameType.MainName)
                inputList.Add(layerInfo.Status!);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return _descriptionDict.ContainsKey(str);
        }

        internal override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions)
        {
            IEnumerable<string> statuses = layerInfos.Select(i => i.Status!).Distinct();
            keywords = statuses.ToArray();
            descriptions = s => _descriptionDict[s];
        }

        internal override void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions)
        {
            keywords = _descriptionDict.Keys.ToArray();
            descriptions = s => _descriptionDict[s];
        }
    }
}
