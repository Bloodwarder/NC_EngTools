using NameClassifiers.Filters;
using NameClassifiers.Highlighting;
using NameClassifiers.Sections;
using NameClassifiers.SharedProperties;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NameClassifiers
{
    public class NameParser
    {
        /// <summary>
        /// Словарь для создания секций парсера
        /// </summary>
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
        private Visualizers? _highliters;

        private List<ParserSection> _sections;
        private NameParser(string xmlPath)
        {
            // Инициализация пути к файлу с данными парсера
            _xmlPath = xmlPath;
            XDocument xDocument = XDocument.Load(xmlPath);

            // Инициализация сепаратора
            XElement separator = xDocument.Root!.Element("Separator") ?? throw new NameParserInitializeException("Отсутствует разделитель");
            Separator = separator.Attribute("Value")!.Value;

            // Проверка наличия элемента с классификаторами
            XElement classifiers = xDocument.Root!.Element("Classifiers") ?? throw new NameParserInitializeException("Ошибка инициализации. Отсутствуют классификаторы");
            ParserSection? prevSection = null;
            if (!classifiers.HasElements)
                throw new NameParserInitializeException("Ошибка инициализации. Отсутствуют классификаторы");

            // Создание цепочки секций парсера для последовательной обработки строк при парсинге строк в LayerInfo и обратной конвертации
            foreach (var classifier in classifiers.Elements())
            {
                string name = classifier.Name.LocalName;
                bool success = Enum.TryParse(name, out Classifier c);
                if (success)
                {
                    // Создать новую секцию по имени Xml элемента (делегат в словаре инициализирует новый объект)
                    var section = _sectionsInitializationDictionary[c](classifier, this);
                    if (Processor != null)
                    {
                        // если секции есть - связать текущую секцию с предыдущей
                        prevSection!.NextSection = section;
                        prevSection = section;
                    }
                    else
                    {
                        // если секций нет - установить текущую секцию как стартовую
                        Processor = section;
                        prevSection = section;
                    }
                }
                else
                {
                    throw new NameParserInitializeException($"Неопознанный классификатор {name}");
                }
            }
            // Проверить состав секций
            if (!ValidateParser())
                throw new NameParserInitializeException("Неправильный состав классификаторов");
            _sections = GetParserSections();
            // Добавить созданный парсер в словарь с загруженными парсерами
            LoadedParsers.Add(Prefix, this);
            LayerWrapper.StandartPrefix ??= Prefix;
        }

        /// <summary>
        /// Загруженные парсеры
        /// </summary>
        public static Dictionary<string, NameParser> LoadedParsers { get; } = new();

        /// <summary>
        /// Данные для фильтрации легенды
        /// </summary>
        public GlobalFilters GlobalFilters
        {
            get
            {
                _globalFilters ??= DeserializeParserData<GlobalFilters>(_xmlPath, "LegendFilters");
                return _globalFilters;
            }
        }

        /// <summary>
        /// Данные общих свойств для групп классификаторов, для облегчения создания слоёв и групп слоёв
        /// </summary>
        public SharedPropertiesCollection SharedProperties
        {
            get
            {
                _sharedPropertiesCollection ??= DeserializeParserData<SharedPropertiesCollection>(_xmlPath, "SharedProperties");
                return _sharedPropertiesCollection;
            }
        }
        /// <summary>
        /// Данные для визуальных фильтров
        /// </summary>
        public Visualizers Visualizers
        {
            get
            {
                _highliters ??= DeserializeParserData<Visualizers>(_xmlPath, "HighlightMode");
                return _highliters;
            }
        }
        /// <summary>
        /// Префикс, используемый для идентификации парсера и для проверки обрабатываемых строк
        /// </summary>
        public string Prefix { get; internal set; } = null!;
        /// <summary>
        /// Разделитель, используемый для декомпозиции строк парсером
        /// </summary>
        public string Separator { get; internal set; } = null!;

        public Dictionary<string, string> AuxilaryDataKeys => AuxilaryData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Description);
        public Dictionary<string, string> AuxilaryClassifiersKeys => AuxilaryData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Description);
        internal PrimaryClassifierSection PrimaryClassifier => FindParserSection<PrimaryClassifierSection>();
        internal Dictionary<string, AuxilaryClassifierSection> AuxilaryClassifiers { get; } = new();
        internal Dictionary<string, AuxilaryDataSection> AuxilaryData { get; } = new();

        internal StatusSection Status => FindParserSection<StatusSection>();
        /// <summary>
        /// Стартовая секция цепочки обработки
        /// </summary>
        internal ParserSection? Processor { get; set; }

        public static NameParser Load(string xmlPath)
        {
            return new NameParser(xmlPath);
        }

        /// <summary>
        /// Парсинг строки имени слоя в объект LayerInfo
        /// </summary>
        /// <param name="layerName">Имя слоя для обработки</param>
        /// <returns></returns>
        public LayerInfo GetLayerInfo(string layerName)
        {
            LayerInfo layerInfo = new(this);
            string[] decomposition = layerName.Split(Separator);

            int pointer = 0;
            // Запускаем обработку строки цепочкой секций парсера
            try
            {
                Processor!.Process(decomposition, layerInfo, pointer);
                return layerInfo;
            }
            catch (IndexOutOfRangeException)
            {
                throw new WrongLayerException($"Ошибка обработки слоя - элементы имени {layerName} заданы неправильно");;
            }
        }


        internal ParserSection GetSection(Type type)
        {
            var section = _sections.Where(s => s.GetType() == type).Single();
            return section;
        }
        internal ParserSection GetSection(Type type, string name)
        {
            var section = _sections.Where(s => s.GetType() == type && s is NamedParserSection nps && nps.Name == name).Single();
            return section;
        }
        internal ParserSection GetSection<T>() where T : ParserSection
        {
            var section = _sections.Where(s => s.GetType() == typeof(T)).Single();
            return (T)section;
        }
        internal ParserSection GetSection<T>(string name) where T : NamedParserSection
        {
            var section = _sections.Where(s => s is NamedParserSection nps && nps.Name == name).Single();
            return (T)section;
        }


        public void ExtractSectionInfo<T>(out string[] keywords, out Func<string, string> descriptions) where T : ParserSection
        {
            GetSection<T>().ExtractFullInfo(out keywords, out descriptions);
        }
        public void ExtractSectionInfo<T>(string name, out string[] keywords, out Func<string, string> descriptions) where T : NamedParserSection
        {
            GetSection<T>(name).ExtractFullInfo(out keywords, out descriptions);
        }
        

        public string[] GetStatusArray() => Status.GetDescriptionDictionary().Keys.ToArray();

        /// <summary>
        /// Десериализовать вспомогательные данные парсера
        /// </summary>
        /// <typeparam name="T">Тип вспомогательных данных</typeparam>
        /// <param name="path">Путь к xml файлу</param>
        /// <param name="elementName">Имя элемента для десериализации (ищется внутри корневого элемента)</param>
        /// <returns></returns>
        /// <exception cref="XmlException"></exception>
        /// <exception cref="IOException"></exception>
        private static T DeserializeParserData<T>(string path, string elementName) where T : class
        {
            XDocument document = XDocument.Load(path);
            var element = document.Root?.Element(elementName) ?? throw new XmlException("Отсутствует корневой элемент");
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (XmlReader reader = element.CreateReader())
            {
                T? result = serializer.Deserialize(reader) as T;
                return result ?? throw new IOException("Не удалось десериализовать объект");
            }
        }

        /// <summary>
        /// Проверка парсера на соответствие секций модели обработки
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Найти нужную секцию в цепочке обработки
        /// </summary>
        /// <typeparam name="TSection">Тип секции</typeparam>
        /// <returns>Искомая секция</returns>
        private TSection FindParserSection<TSection>() where TSection : ParserSection
        {
            ParserSection section = Processor!;
            while (section is not TSection)
            {
                section = section.NextSection!;
            }
            TSection resultSection = (TSection)section;
            return resultSection;
        }

        private List<ParserSection> GetParserSections()
        {
            ParserSection section = Processor!;

            List<ParserSection> sections = new() { section };
            while (section.NextSection != null)
            {
                section = section.NextSection!;
                sections.Add(section);
            }
            return sections;
        }
    }
}
