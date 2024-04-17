using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    internal class StatusSection : ParserSection
    {
        private Dictionary<string, string> _descriptionDict = new();
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

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (!_descriptionDict.ContainsKey(str[pointer]))
                throw new WrongLayerException("Не найден статус");
            layerInfo.Status = str[pointer];
            pointer++;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {
            inputList.Add(layerInfo.Status);
            NextSection?.ComposeName(inputList, layerInfo);
        }
        internal override bool ValidateString(string str)
        {
            return _descriptionDict.ContainsKey(str);
        }
    }
}
