using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Дополнительная информация. Необязательная. Независим от положения (только от скобок).
    /// Может быть несколько (с разными скобками). Не считается частью основного имени
    /// </summary>
    internal class AuxilaryDataSection : ParserSection
    {
        internal char[] Brackets { get; init; }
        internal string Name { get; init; }
        internal string Description { get; init; }
        internal AuxilaryDataValidation? Validation { get; init; }

        public AuxilaryDataSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
            XAttribute nameAttr = xElement.Attribute("Name") ?? throw new NameParserInitializeException("Отсутствует ключ для дополнительных данных");
            XAttribute bracketsAttr = xElement.Attribute("Brackets") ?? throw new NameParserInitializeException("Отсутствуют скобки для дополнительных данных");
            XAttribute descriptionAttr = xElement.Attribute("Description") ?? throw new NameParserInitializeException("Отсутcтвует описание дополнительных данных");
            XElement? validationElement = xElement.Element("Validation");
            if (validationElement != null)
                Validation = new AuxilaryDataValidation(validationElement);
            Name = nameAttr.Value;
            Description = descriptionAttr.Value;
            Brackets = new[] { bracketsAttr.Value[0], bracketsAttr.Value[1] };
            parentParser.AuxilaryData.Add(nameAttr.Value, this);
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Проверить текущую строку массива на начало с нужной скобки, если нет, проверить следующий классификатор
            if (!str[pointer].StartsWith(Brackets[0]))
            {
                layerInfo.AuxilaryData[Name] = null;
                NextSection?.Process(str, layerInfo, pointer);
                return;
            }
            // Если да - установить счётчик элементов на 1 и увеличивать пока не будет найдена закрывающая скобка
            int elementsCounter = 1;
            while (!str[pointer + elementsCounter - 1].EndsWith(Brackets[1]))
            {
                elementsCounter++;
            }
            // Объединить строки, переместить указатель вперёд на число полученных элементов
            string auxData = string.Join(ParentParser.Separator, str.Skip(pointer - 1).Take(elementsCounter).ToArray());
            string formattedAuxData = auxData.Replace(Brackets[0].ToString(), "").Replace(Brackets[1].ToString(), "");
            layerInfo.AuxilaryData[Name] = formattedAuxData;
            pointer += elementsCounter;

            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            // Дополнительные данные только для полного имени
            if (nameType == NameType.FullName)
            {
                bool success = layerInfo.AuxilaryData.TryGetValue(Name, out string? data);
                if (success && data != null)
                    inputList.Add($"{Brackets[0]}{data}{Brackets[1]}");
            }
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }
        internal override bool ValidateString(string str)
        {
            return str.StartsWith(Brackets[0]);
        }
    }
}
