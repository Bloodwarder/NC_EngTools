using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Основной классификатор. Обязательный. Независим от положения. Может быть только один. Считается частью основного имени
    /// </summary>
    public class PrimaryClassifierSection : ParserSection
    {
        private Dictionary<string, string> _descriptionDict = new();
        public PrimaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            foreach (XElement chapter in xElement.Elements("Chapter"))
            {
                XAttribute? keyAttr = chapter.Attribute("Value");
                XAttribute? descriptionAttr = chapter.Attribute("Description");

                if (keyAttr != null && descriptionAttr != null)
                    _descriptionDict.Add(keyAttr.Value, descriptionAttr.Value);
                else
                    throw new NameParserInitializeException("Ошибка инициализации основного классификатора. Неверный ключ или описание");
            }
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            layerInfo.PrimaryClassifier = str[pointer];
            pointer++;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            inputList.Add(layerInfo.PrimaryClassifier!);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return _descriptionDict.ContainsKey(str);
        }
    }
}
