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
            XElement? validationElement = xElement.Element("Validation");
            if (validationElement != null)
            {
                XmlSerializer serializer = new(typeof(LayerValidator));
                using (var reader = validationElement.CreateReader())
                {
                    while (reader.Read())
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

        internal abstract void Process(string[] str, LayerInfo layerInfo, int pointer);
        internal abstract void ComposeName(List<string> inputList, LayerInfo layerInfo, NameType nameType);
        internal abstract bool ValidateString(string str);
        internal abstract void ExtractDistinctInfo(IEnumerable<LayerInfo> layerInfos, out string[] keywords, out Func<string, string> descriptions);
        internal abstract void ExtractFullInfo(out string[] keywords, out Func<string, string> descriptions);
    }
}
