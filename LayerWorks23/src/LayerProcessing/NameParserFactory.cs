using LayerWorks.LayerProcessing;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml.Linq;

namespace LayerWorks23.LayerProcessing
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
            [Classifier.AuxilaryClassifier] = InitializeAuxilaryData,
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
                throw new Exception("NameParser с указанным префиксом уже существует");
            // Задаём префикс парсеру
            _parser.Prefix = prefixValue;
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier с = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[с]);
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

            // Добавляем в очередь обработки строки соответствующий метод, если он уже не был добавлен
            // (обработка дополнительной информации идёт только один раз)
            if (!_parser.IsAuxilaryDataInitialized)
            {
                Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
                _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
                _parser.IsAuxilaryDataInitialized = true;
            }
        }

        private static void InitializeSecondaryClassifiers(XElement element)
        {
            // Добавляем в очередь обработки строки соответствующий метод
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
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
            foreach (XElement classifier in element.Elements())
            {
                _parser.StatusClassifiers.Add(classifier.Value, classifier.Attribute("Description").Value);
            }
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
            _parser.IsStatusInitialized = true;
        }

        public static void InitializeBooleanSuffix(XElement element)
        {
            _parser.Suffix = element.Attribute("Value").Value;
            Classifier c = Enum.Parse<Classifier>(element.Name.ToString());
            _parser.ProcessingQueue.Add(_parser.ProcessingDictionary[c]);
        }
    }



    public class NameParser
    {
        public static Dictionary<string, NameParser> LoadedParsers { get; } = new();
        internal Dictionary<Classifier, Func<string[], LayerInfo, int, int>> ProcessingDictionary { get; }


        internal NameParser()
        {
            ProcessingDictionary = new()
            {
                [Classifier.Prefix] = ProcessPrefix,
                [Classifier.PrimaryClassifier] = ProcessPrimaryClassifier,
                [Classifier.AuxilaryClassifier] = ProcessAuxilaryClassifier,
                [Classifier.AuxilaryData] = ProcessAuxilaryData,
                [Classifier.SecondaryClassifiers] = ProcessSecondaryClassifiers,
                [Classifier.StatusClassifier] = ProcessStatusClassifier,
                [Classifier.BooleanSuffix] = ProcessBooleanSuffix
            };
        }


        internal List<Func<string[], LayerInfo, int, int>> ProcessingQueue { get; } = new();
        internal Dictionary<string, string> PrimaryClassifiers { get; } = new();
        internal Dictionary<string, string> StatusClassifiers { get; } = new();
        internal List<Dictionary<string, string>> AuxilaryClassifiers { get; } = new();
        internal List<string> AuxilaryDataKeys { get; } = new();
        internal List<char[]> AuxilaryDataBrackets { get; } = new();
        internal Dictionary<string, Dictionary<string, string>> StatusTransformations { get; } = new();
        internal Dictionary<string, HashSet<string>> ValidPrimary { get; } = new();
        internal Dictionary<string, HashSet<string>> ValidAuxilary { get; } = new();
        internal string Prefix { get; set; }
        internal string Suffix { get; set; }
        internal string Separator { get; set; }

        internal bool IsPrefixInitialized = false;
        internal bool IsPrimaryInitialized = false;
        internal bool IsAuxilaryDataInitialized = false;
        internal bool IsSecondaryInitialized = false;
        internal bool IsStatusInitialized = false;
        internal bool InitializedWithErrors = false;
        internal List<string> ErrorMessages { get; } = new();


        public LayerInfo GetLayerInfo(string layerName)
        {
            LayerInfo layerInfo = new(this);
            string[] decomposition = layerName.Split(Separator);

            int pointer = 0;
            foreach (Func<string[], LayerInfo, int, int> func in ProcessingQueue)
            {
                pointer = func(decomposition, layerInfo, pointer);
            }
            return layerInfo;
        }

        public int ProcessPrefix(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (str[pointer] != layerInfo.ParentParser.Prefix)
                throw new WrongLayerException("Не совпадает префикс");
            layerInfo.Prefix = layerInfo.ParentParser.Prefix;
            pointer++;
            return pointer;
        }

        public int ProcessPrimaryClassifier(string[] str, LayerInfo layerInfo, int pointer)
        {
            layerInfo.PrimaryClassifier = str[pointer];
            pointer++;
            return pointer;
        }

        public int ProcessAuxilaryClassifier(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (AuxilaryClassifiers[0].ContainsKey(str[pointer]))
                layerInfo.AuxilaryClassifiers.Add(str[pointer]);
            pointer++;
            return pointer;
        }

        public int ProcessAuxilaryData(string[] str, LayerInfo layerInfo, int pointer)
        {
            for (int i = 0; i < AuxilaryDataBrackets.Count; i++)
            {
                // Проверить строку на 
                if (!str[pointer].StartsWith(AuxilaryDataBrackets[i][0]))
                {
                    layerInfo.AuxilaryData[AuxilaryDataKeys[i]] = null;
                    continue;
                }
                int elementsCounter = 1;
                while (!str[pointer].EndsWith(AuxilaryDataBrackets[i][1]))
                {
                    elementsCounter++;
                }
                string auxData = string.Join(Separator, str.Skip(pointer - 1).Take(elementsCounter).ToArray());
                string formattedAuxData = auxData.Replace(AuxilaryDataBrackets[i][0].ToString(), "").Replace(AuxilaryDataBrackets[i][1].ToString(), "");
                layerInfo.AuxilaryData[AuxilaryDataKeys[i]] = formattedAuxData;
                pointer += elementsCounter;
            }
            return pointer;
        }

        public int ProcessSecondaryClassifiers(string[] str, LayerInfo layerInfo, int pointer)
        {
            int elementsCounter = 0;
            while (!StatusClassifiers.ContainsKey(str[pointer]))
            {
                elementsCounter++;
            }
            string secondary = string.Join(Separator, str.Skip(pointer - 1).Take(elementsCounter).ToArray());
            pointer += elementsCounter;
            layerInfo.SecondaryClassifiers = secondary;
            return pointer;
        }

        public int ProcessStatusClassifier(string[] str, LayerInfo layerInfo, int pointer)
        {
            if (!StatusClassifiers.ContainsKey(str[pointer]))
                throw new WrongLayerException("Не найден статус");
            layerInfo.Status = str[pointer];
            pointer++;
            return pointer;
        }

        public int ProcessBooleanSuffix(string[] str, LayerInfo layerInfo, int pointer)
        {
            try
            {
                layerInfo.SuffixTagged = str[pointer] == Suffix;
            }
            catch
            { }
            pointer++;
            return pointer;
        }

    }

    public class LayerInfo
    {
        public NameParser ParentParser { get; }

        public string Prefix { get; internal set; }
        public string PrimaryClassifier { get; internal set; }
        public List<string> AuxilaryClassifiers { get; } = new();
        public Dictionary<string, string> AuxilaryData { get; }
        public string SecondaryClassifiers { get; set; }
        public string Status { get; set; }
        public bool SuffixTagged { get; set; }

        public string MainName => string.Concat(PrimaryClassifier,
                                                ParentParser.Separator,
                                                AuxilaryClassifiers,
                                                ParentParser.Separator,
                                                SecondaryClassifiers);
        public string TrueName => string.Concat(PrimaryClassifier,
                                                ParentParser.Separator,
                                                AuxilaryClassifiers,
                                                ParentParser.Separator,
                                                SecondaryClassifiers,
                                                ParentParser.Separator,
                                                Status);
        public LayerInfo(NameParser parser)
        {
            ParentParser = parser;
        }

        public void SwitchStatus(string newStatus)
        {
            if (ParentParser.StatusClassifiers.ContainsKey(newStatus))
                Status = newStatus;
        }
        public void ChangeAuxilaryData(string key, string value)
        {
            if (!ParentParser.ValidPrimary[key].Contains(PrimaryClassifier))
                throw new Exception($"Нельзя назначить {key} для объекта типа \"{ParentParser.PrimaryClassifiers[PrimaryClassifier]}\"");
            bool validAuxilary = false;
            foreach (string aux in AuxilaryClassifiers)
                if (ParentParser.ValidAuxilary[key].Contains(aux))
                {
                    validAuxilary = true;
                    break;
                }
            if (!validAuxilary)
            {
                throw new Exception($"Нельзя назначить {key}. Дополнительного классификатора нет в списке допустимых");
            }
            if (ParentParser.StatusTransformations[key].ContainsKey(Status))
                Status = ParentParser.StatusTransformations[key][Status];
            AuxilaryData[key] = value;
        }

    }

    internal enum Classifier
    {
        Prefix,
        PrimaryClassifier,
        AuxilaryClassifier,
        SecondaryClassifiers,
        AuxilaryData,
        StatusClassifier,
        BooleanSuffix
    }
}
