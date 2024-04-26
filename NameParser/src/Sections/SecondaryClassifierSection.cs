using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Сборный классификатор, собирающий все значения, не вошедшие в остальные классификаторы
    /// </summary>
    internal class SecondaryClassifierSection : ParserSection
    {
        public SecondaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Установить счётчик элементов на 0 и увеличивать пока не будет найден элемент, содержащий статус,
            // затем передать значения и переместить указатель вперёд
            int elementsCounter = 0;
            while (NextSection != null && !NextSection.ValidateString(str[pointer + elementsCounter]))
            {
                elementsCounter++;
                if (pointer + elementsCounter > str.Length)
                    break;
            }
            string secondary = string.Join(ParentParser.Separator, str.Skip(pointer - 1).Take(elementsCounter).ToArray());
            pointer += elementsCounter;
            layerInfo.SecondaryClassifiers = secondary;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {
            inputList.Add(layerInfo.SecondaryClassifiers);
            NextSection?.ComposeName(inputList, layerInfo);
        }

        internal override bool ValidateString(string str)
        {
            // если значение не является подходящим для следующей секции, значит подходит для этой, собирающейся по остаточному принципу
            return !(NextSection?.ValidateString(str) ?? true);
        }
    }
}
