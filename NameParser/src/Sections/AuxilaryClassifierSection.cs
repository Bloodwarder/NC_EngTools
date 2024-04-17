using System.Xml.Linq;

namespace NameClassifiers.Sections
{
    internal class AuxilaryClassifierSection : ParserSection
    {
        public AuxilaryClassifierSection(XElement xElement, NameParser parentParser) : base(xElement, parentParser)
        {
        }



        internal override void Process(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Проверяем наличие текущего элемента массива в словарях дополнительных классификаторов. Если есть, добавляем в layerInfo
            // Если нет - проверяем добавляем null и проверяем следующий словарь
            for (int i = 0; i < layerInfo.ParentParser.AuxilaryClassifiers.Count; i++)
            {
                if (layerInfo.ParentParser.AuxilaryClassifiers[i].ContainsKey(str[pointer]))
                {
                    layerInfo.AuxilaryClassifiers.Add(str[pointer]);
                    pointer++;
                }
                else
                {
                    layerInfo.AuxilaryClassifiers.Add(null);
                }
            }
            NextSection?.Process(str, layerInfo, pointer);
        }
        internal override void ComposeName(List<string> inputList, LayerInfo layerInfo)
        {

            NextSection?.ComposeName(inputList, layerInfo);
        }
        internal override bool ValidateString(string str)
        {
            throw new NotImplementedException();
        }
    }
}
