using System.Xml;
using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Дополнительный классификатор. Необязательный. Независим от положения. Может быть несколько. Считается частью основного имени
    /// </summary>
    public class AuxilaryClassifierSection : NamedParserSection
    {
        private readonly Dictionary<string, string> _descriptionDict = new();
        private readonly bool _isRequired;
        internal string Description { get; init; }
        public AuxilaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute classifierDescriptionAttr = xElement.Attribute("Description") ?? throw new NameParserInitializeException("Отсутcтвует описание дополнительного классификатора");
            Description = classifierDescriptionAttr.Value;

            bool isRequired = XmlConvert.ToBoolean(xElement.Attribute("Required")?.Value ?? "true");
            _isRequired = isRequired;

            foreach (XElement classifier in xElement.Elements("Classifier"))
            {
                XAttribute? keyAttr = classifier.Attribute("Value");
                XAttribute? descriptionAttr = classifier.Attribute("Description");

                if (keyAttr != null && descriptionAttr != null)
                    _descriptionDict.Add(keyAttr.Value, descriptionAttr.Value);
                else
                    throw new NameParserInitializeException("Ошибка инициализации дополнительного классификатора. Неверный ключ или описание");
            }
            ParentParser.AuxilaryClassifiers.Add(Name, this);
        }

        internal override void Process(string[] str, LayerInfoResult layerInfoResult, ref int pointer)
        {
            // Проверяем наличие текущего элемента массива в словарях дополнительных классификаторов. Если есть, добавляем в layerInfo
            // Если нет - проверяем добавляем null и проверяем следующий словарь
            if (_descriptionDict.ContainsKey(str[pointer]))
            {
                layerInfoResult.Value!.AuxilaryClassifiers.Add(Name, str[pointer]);
                pointer++;
            }
            else if (_isRequired)
            {
                layerInfoResult.Exceptions.Add(new WrongLayerException($"Классификатор \"{str[pointer]}\" отсутствует в списке допустимых"));
                layerInfoResult.Status = LayerInfoParseStatus.PartialFailure;
            }
            else
            {
                layerInfoResult.Value!.AuxilaryClassifiers.Add(Name, null);
            }
            NextSection?.Process(str, layerInfoResult, ref pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            string? classifier = layerInfo.AuxilaryClassifiers[Name];
            if (classifier != null)
                inputList.Add(classifier);
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

        internal override void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions)
        {
            keywords = _descriptionDict.Keys.ToArray();
            descriptions = s => _descriptionDict[s];
        }
    }
}
