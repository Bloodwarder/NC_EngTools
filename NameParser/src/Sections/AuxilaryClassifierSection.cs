﻿using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    internal class AuxilaryClassifierSection : ParserSection
    {
        private Dictionary<string, string> _descriptionDict = new();
        public AuxilaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            Name = xElement.Attribute("Name")?.Value ?? throw new NameParserInitializeException("Отсутствует имя дополнительного классификатора");
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

        internal string Name { get; init; }


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
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {
            inputList.Add(layerInfo.AuxilaryClassifiers[Name]);
            NextSection?.ComposeName(inputList, layerInfo);
        }
        internal override bool ValidateString(string str)
        {
            throw new NotImplementedException();
        }
    }
}
