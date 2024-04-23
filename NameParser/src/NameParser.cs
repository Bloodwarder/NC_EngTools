


using NameClassifiers.Sections;
using System.Xml.Linq;

namespace NameClassifiers
{
    public class NameParser
    {

        private static readonly Dictionary<Classifier, Func<XElement, NameParser, ParserSection>> _sectionsInitializationDictionary = new()
        {
            [Classifier.Prefix] = (x, p) => new PrefixSection(x, p),
            [Classifier.PrimaryClassifier] = (x, p) => new PrimaryClassifierSection(x, p),
            [Classifier.AuxilaryClassifier] = (x, p) => new AuxilaryClassifierSection(x, p),
            [Classifier.AuxilaryData] = (x, p) => new AuxilaryDataSection(x, p),
            [Classifier.SecondaryClassifiers] = (x, p) => new SecondaryClassifierSection(x, p),
            [Classifier.StatusClassifier] = (x, p) => new StatusSection(x, p),
            [Classifier.BooleanSuffix] = (x, p) => new BooleanSection(x, p)
        };


        public NameParser(XDocument xDocument)
        {
            XElement separator = xDocument.Root!.Element("Separator") ?? throw new NameParserInitializeException("Отсутствует разделитель");
            Separator = separator.Attribute("Value").Value;
            XElement classifiers = xDocument.Root!.Element("Classifiers") ?? throw new NameParserInitializeException("Ошибка инициализации. Отсутствуют классификаторы");
            ParserSection? prevSection = null;
            if (!classifiers.HasElements)
                throw new NameParserInitializeException("Ошибка инициализации. Отсутствуют классификаторы");
            foreach (var classifier in classifiers.Elements())
            {
                string name = classifier.Name.LocalName;
                bool success = Enum.TryParse(name, out Classifier c);
                if (success)
                {
                    var section = _sectionsInitializationDictionary[c](classifier, this);
                    if (Processor != null)
                    {
                        prevSection!.NextSection = section;
                        prevSection = section;
                    }
                    else
                    {
                        Processor = section;
                        prevSection = section;
                    }
                }
                else
                {
                    throw new NameParserInitializeException($"Неопознанный классификатор {name}");
                }
            }
        }

        public string Prefix { get; internal set; } = null!;
        public string? Suffix { get; internal set; }
        public string Separator { get; internal set; }
        public static Dictionary<string, NameParser> LoadedParsers { get; } = new();

        internal PrimaryClassifierSection PrimaryClassifier { get; set; }
        internal Dictionary<string, AuxilaryClassifierSection> AuxilaryClassifiers = new();
        internal Dictionary<string, AuxilaryDataSection> AuxilaryData = new();
        internal StatusSection Status { get; set; }
        internal ParserSection? Processor { get; set; }

        public LayerInfo GetLayerInfo(string layerName)
        {
            LayerInfo layerInfo = new(this);
            string[] decomposition = layerName.Split(Separator);

            int pointer = 0;
            // Вызываем обрабатывающие методы из очереди
            Processor!.Process(decomposition, layerInfo, pointer);
            return layerInfo;
        }

    }
}
