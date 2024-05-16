


using NameClassifiers.Filters;
using NameClassifiers.Sections;
using NameClassifiers.SharedProperties;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

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
        private string _xmlPath;
        private GlobalFilters? _globalFilters;
        private SharedPropertiesCollection? _sharedPropertiesCollection;

        public NameParser(string xmlPath)
        {
            _xmlPath = xmlPath;
            XDocument xDocument = XDocument.Load(xmlPath);
            XElement separator = xDocument.Root!.Element("Separator") ?? throw new NameParserInitializeException("Отсутствует разделитель");
            Separator = separator.Attribute("Value")!.Value;
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
            if (!ValidateParser())
                throw new NameParserInitializeException("Неправильный состав классификаторов");
            LoadedParsers.Add(Prefix, this);
            LayerWrapper.StandartPrefix ??= Prefix;
        }
        public GlobalFilters GlobalFilters
        {
            get
            {
                _globalFilters ??= DeserializeParserData<GlobalFilters>(_xmlPath,"LegendFilters");
                return _globalFilters;
            }
        }
        public SharedPropertiesCollection SharedProperties 
        {
            get
            {
                _sharedPropertiesCollection ??= DeserializeParserData<SharedPropertiesCollection>(_xmlPath, "SharedProperties");
                return _sharedPropertiesCollection;
            }
        }

        public string Prefix { get; internal set; } = null!;
        public string? Suffix { get; internal set; }
        public string Separator { get; internal set; } = null!;
        public static Dictionary<string, NameParser> LoadedParsers { get; } = new();

        internal PrimaryClassifierSection PrimaryClassifier { get; set; } = null!;
        internal Dictionary<string, AuxilaryClassifierSection> AuxilaryClassifiers = new();
        internal Dictionary<string, AuxilaryDataSection> AuxilaryData = new();
        internal StatusSection Status { get; set; } = null!;
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

        private bool ValidateParser()
        {
            List<ParserSection> sections = new();
            var currentSection = Processor;
            do
            {
                sections.Add(currentSection!);
                currentSection = currentSection!.NextSection;
            }
            while (currentSection!.NextSection != null);
            var prefixSections = sections.Where(s => s is PrefixSection);
            bool prefixInitialized = prefixSections.Count() == 1 && sections.IndexOf(prefixSections.First()) == 0;
            bool primaryInitialized = sections.Where(s => s is PrimaryClassifierSection).Count() == 1;
            bool secondaryInitialized = sections.Any(s => s is SecondaryClassifierSection);
            bool statusInitialized = sections.Where(s => s is StatusSection).Count() == 1;
            return prefixInitialized && primaryInitialized && secondaryInitialized && statusInitialized;
        }

        private T DeserializeParserData<T>(string path, string elementName) where T : class
        {
            XDocument document = XDocument.Load(path);
            var element = document.Root?.Element(elementName) ?? throw new Exception("Отсутствует корневой элемент");
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using(XmlReader reader = element.CreateReader())
            {
                T? result = serializer.Deserialize(reader) as T;
                return result ?? throw new Exception("Не удалось десериализовать объект");
            }
        }
    }
}
