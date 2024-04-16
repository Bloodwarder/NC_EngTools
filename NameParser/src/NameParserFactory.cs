using System.Xml.Linq;

namespace NameClassifiers
{
    internal static class NameParserFactory
    {
        private static NameParser _parser = null;
        internal static NameParser Create(string xmlPath)
        {
            // Создаём пустой парсер
            _parser = new();

            // Загружаем классификаторы из xml
            XDocument xDoc = XDocument.Load(xmlPath);
            _parser.Separator = xDoc.Root.Element("Separator").Value;
            XElement classifiers = xDoc.Root.Element("Classifiers");

            // Для каждого классификатора вызываем соответствующий метод инициализации из словаря
            foreach (XElement classifier in classifiers.Elements())
            {
                Classifier c = Enum.Parse<Classifier>(classifier.Name.ToString());
                
                InitializingDictionary[c](classifier);
            }

            return _parser;
        }

        private static Dictionary<Classifier, Action<XElement>> InitializingDictionary { get; } = new()
        {
            [Classifier.Prefix] = InitializePrefix,
            [Classifier.PrimaryClassifier] = InitializePrimaryClassifier,
            [Classifier.AuxilaryClassifier] = InitializeAuxilaryClassifier,
            [Classifier.AuxilaryData] = InitializeAuxilaryData,
            [Classifier.SecondaryClassifiers] = InitializeSecondaryClassifiers,
            [Classifier.StatusClassifier] = InitializeStatusClassifier,
            [Classifier.BooleanSuffix] = InitializeBooleanSuffix
        };

        private static void InitializePrefix(XElement element)
        {
            // Проверяем не был ли префикс уже инициализирован
            if (_parser.IsPrefixInitialized)
            {
                _parser.InitializedWithErrors = true;
                _parser.ErrorMessages.Add("Префикс задан более одного раза. Второй и последующие префиксы проигнорированы");
                return;
            }
            string prefixValue = element.Attribute("Value").Value;

            // Проверяем, не было ли уже указанного префикса в словаре парсеров
            bool successAdd = NameParser.LoadedParsers.TryAdd(prefixValue, _parser);
            if (!successAdd)
                throw new NameParserInitializeException("NameParser с указанным префиксом уже существует");
            // Задаём префикс парсеру
            _parser.Prefix = prefixValue;
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
            // Указываем, что префикс инициализирован
            _parser.IsPrefixInitialized = true;
        }

        private static void InitializePrimaryClassifier(XElement element)
        {
            // Добавляем элементы в словарь основного классификатора
            foreach (XElement classifier in element.Elements())
            {
                _parser.PrimaryClassifiers.Add(classifier.Attribute("Value").Value, classifier.Attribute("Description").Value);
            }

            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
            // Указываем, что основной классификатор инициализирован
            _parser.IsPrimaryInitialized = true;
        }

        private static void InitializeAuxilaryClassifier(XElement element)
        {
            // Добавляем новый словарь дополнительного классификатора и добавляем в него значения
            Dictionary<string, string> dict = new();
            foreach (XElement auxilary in element.Elements("Classifier"))
                dict.Add(auxilary.Attribute("Value").Value, auxilary.Attribute("Description").Value);
            _parser.AuxilaryClassifiers.Add(dict);
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
        }

        private static void InitializeAuxilaryData(XElement element)
        {
            // Проверяем, были ли уже использованы скобки указанного типа для другого классификатора
            string brackets = element.Attribute("Brackets").Value;
            foreach (char bracket in _parser.AuxilaryDataBrackets.Select(b => b[0]).ToArray())
            {
                if (brackets[0] == bracket)
                {
                    _parser.InitializedWithErrors = true;
                    _parser.ErrorMessages.Add("Дополнительная информация не инициализирована - скобки уже встречаются в другом классификаторе");
                    return;
                }
            }
            _parser.AuxilaryDataBrackets.Add(brackets.ToArray());
            // Также проверяем ключи
            string key = element.Attribute("Name").Value;
            if (_parser.AuxilaryDataKeys.Contains(key))
            {
                _parser.InitializedWithErrors = true;
                _parser.ErrorMessages.Add("Дополнительная информация не инициализирована - дублирующиеся имена");
            }
            _parser.AuxilaryDataKeys.Add(key);
            // Если дополнительная информация правомерна только для определённых статусов - заполняем словари трансформации
            XElement validStatuses = element.Element("ValidStatuses");
            if (validStatuses != null)
            {
                foreach (XElement elem in validStatuses.Elements("StatusReference"))
                {
                    foreach (XElement sourceStatus in elem.Elements("StatusReference"))
                    {
                        _parser.StatusTransformations[key][sourceStatus.Attribute("Value").Value] = sourceStatus.Parent.Attribute("Value").Value;
                    }
                }
            }
            // Если дополнительная информация правомерна только для определённых основных и дополнительных классификаторов -
            // заполняем коллекции с правомерными значениями
            XElement validPrimary = element.Element("ValidPrimary");
            if (validPrimary != null)
            {
                HashSet<string> validSet = new();
                foreach (XElement elem in validPrimary.Elements("ChapterReference"))
                    validSet.Add(elem.Attribute("Value").Value);
                _parser.ValidPrimary[key] = validSet;
            }

            XElement validAuxilary = element.Element("ValidAuxilary");
            if (validAuxilary != null)
            {
                HashSet<string> validSet = new();
                foreach (XElement elem in validPrimary.Elements("ClassifierReference"))
                    validSet.Add(elem.Attribute("Value").Value);
                _parser.ValidPrimary[key] = validSet;
            }

            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            // Добавляем в очередь обработки строки соответствующий метод, если он уже не был добавлен
            // (обработка дополнительной информации идёт только один раз)
            if (!_parser.IsAuxilaryDataInitialized)
            {
                _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
                _parser.IsAuxilaryDataInitialized = true;
            }
        }

        private static void InitializeSecondaryClassifiers(XElement element)
        {
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
            // Указываем, что вторичные классификаторы инициализированы
            _parser.IsSecondaryInitialized = true;
        }

        private static void InitializeStatusClassifier(XElement element)
        {
            // Проверяем не был ли статус уже инициализирован
            if (_parser.IsStatusInitialized)
            {
                _parser.InitializedWithErrors = true;
                _parser.ErrorMessages.Add("Статус задан более одного раза");
                return;
            }
            // Заполняем словарь
            foreach (XElement classifier in element.Elements())
            {
                _parser.StatusClassifiers.Add(classifier.Value, classifier.Attribute("Description").Value);
            }
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
            _parser.AssembleOrder.Add(c);
            // Указываем, что статус инициализирован
            _parser.IsStatusInitialized = true;
        }

        public static void InitializeBooleanSuffix(XElement element)
        {
            _parser.Suffix = element.Attribute("Value").Value;
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.AssembleOrder.Add(c);
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
        }
    }
}
