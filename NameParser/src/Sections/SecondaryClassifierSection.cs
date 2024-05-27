﻿using System.Xml.Linq;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Вторичный классификатор, собирающий все значения, не вошедшие в остальные классификаторы.
    /// Обязательный. Может быть только один. Является частью основного имени
    /// </summary>
    public class SecondaryClassifierSection : ParserSection
    {
        public SecondaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
        }

        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Установить счётчик элементов на 0 и увеличивать пока не будет найден элемент, содержащий следующий классификатор,
            // затем передать значения и переместить указатель вперёд
            int elementsCounter = 0;
            while (NextSection != null && !NextSection.ValidateString(str[pointer + elementsCounter]))
            {
                elementsCounter++;
                if (pointer + elementsCounter > str.Length)
                    break;
            }
            string secondary = string.Join(ParentParser.Separator, str.Skip(pointer).Take(elementsCounter).ToArray());
            pointer += elementsCounter;
            layerInfo.SecondaryClassifiers = secondary;
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType)
        {
            // Добавляется в любое имя - проверку nameType не производим
            inputList.Add(layerInfo.SecondaryClassifiers!);
            NextSection?.ComposeName(inputList, layerInfo, nameType);
        }

        internal override bool ValidateString(string str)
        {
            // если значение не является подходящим для следующей секции, значит подходит для этой, собирающейся по остаточному принципу
            return !(NextSection?.ValidateString(str) ?? true);
        }
    }
}
