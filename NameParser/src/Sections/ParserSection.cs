using NameClassifiers.References;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using System.Xml.Serialization;
using static NameClassifiers.LayerInfo;

namespace NameClassifiers.Sections
{
    /// <summary>
    /// Секция классификатора. Секции последовательно собираются в цепочку обязанностей 
    /// в конструкторе NameParser и последовательно обрабатывают входящие массивы строк или объекты LayerInfo.
    /// </summary>
    public abstract class ParserSection
    {

        protected ParserSection(XElement xElement, NameParser parentParser)
        {
            this.ParentParser = parentParser;
            InitializeValidators(xElement);

        }

        private void InitializeValidators(XElement xElement)
        {
            IEnumerable<XElement>? validators = xElement.Element("Validation")?.Elements();
            if (validators != null)
            {
                XmlSerializer serializer = new(typeof(LayerValidator));
                foreach (XElement validator in validators)
                {
                    using (var reader = validator.CreateReader())
                    {
                        LayerValidator? result = serializer.Deserialize(reader) as LayerValidator;
                        Validators.Add(result ?? throw new IOException("Не удалось десериализовать объект"));
                    }
                }
            }
        }

        internal ParserSection? NextSection { get; set; }
        internal protected List<LayerValidator> Validators { get; private set; } = new();
        protected NameParser ParentParser { get; init; }

        internal abstract void Process(string[] str, LayerInfoResult layerInfo, int pointer);
        internal abstract void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType);
        internal abstract bool ValidateString(string str);
        internal abstract void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions);
        internal abstract void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions);
    }
}
