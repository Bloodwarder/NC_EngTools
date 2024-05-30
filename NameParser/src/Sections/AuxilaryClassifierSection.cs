using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Дополнительный классификатор. Необязательный. Независим от положения. Может быть несколько. Считается частью основного имени
    /// </summary>
    public class AuxilaryClassifierSection : NamedParserSection
    {
        private Dictionary<string, string> _descriptionDict { get; } = new();
        public AuxilaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            foreach (XElement classifier in xElement.Elements("Classifier"))
            {
                XAttribute? keyAttr = classifier.Attribute("Value");
                XAttribute? descriptionAttr = classifier.Attribute("Description");

                if (keyAttr != null && descriptionAttr != null)
                    _descriptionDict.Add(keyAttr.Value, descriptionAttr.Value);
                else
                    throw new NameParserInitializeException("Ошибка инициализации дополнительного классификатора. Неверный ключ или описание");
            }
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Проверяем наличие текущего элемента массива в словарях дополнительных классификаторов. Если есть, добавляем в layerInfo
            // Если нет - проверяем добавляем null и проверяем следующий словарь
            if (_descriptionDict.ContainsKey(str[pointer]))
            {
                layerInfo.AuxilaryClassifiers.Add(Name, str[pointer]);
                pointer++;
            }
            else
            {
                throw new WrongLayerException($"Классификатор \"{str[pointer]}\" отсутствует в списке допустимых");
                //layerInfo.AuxilaryClassifiers.Add(Name, null);
            }
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {

            inputList.Add(layerInfo.AuxilaryClassifiers[Name]);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return _descriptionDict.ContainsKey(str);
        }

        internal override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptionFunc)
        {
            var classifiers = layerInfos.Select(i => i.AuxilaryClassifiers[Name]).Distinct();
            keywords = classifiers.ToArray();
            descriptionFunc = s => s;
        }
    }
}
