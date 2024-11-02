using System.Xml.Linq;
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

        internal override void Process(string[] str, LayerInfoResult layerInfoResult, int pointer)
        {
            // Установить счётчик элементов на 0 и увеличивать пока не будет найден элемент, содержащий следующий классификатор,
            // затем передать значения и переместить указатель вперёд
            int elementsCounter = 0;
            if (NextSection != null)
            {
                do
                {
                    elementsCounter++;
                    if (pointer + elementsCounter > str.Length - 1)
                        break;
                } while (!NextSection.ValidateString(str[pointer + elementsCounter]));
            }
            string secondary = string.Join(ParentParser.Separator, str.Skip(pointer).Take(elementsCounter).ToArray());
            pointer += elementsCounter;
            layerInfoResult.Value.SecondaryClassifiers = secondary;
            NextSection?.Process(str, layerInfoResult, pointer);
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

        internal override void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions)
        {
            throw new NotImplementedException("Вторичный классификатор не используется в этом контексте");
        }

        internal override void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions)
        {
            keywords = Array.Empty<string>();
            descriptions = s => string.Empty;
        }
    }
}
