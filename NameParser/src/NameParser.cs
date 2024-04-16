

namespace NameClassifiers
{
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
        internal List<Classifier> AssembleOrder { get; } = new();
        internal Dictionary<string, string> PrimaryClassifiers { get; } = new();
        internal Dictionary<string, string> StatusClassifiers { get; } = new();
        internal List<Dictionary<string, string>> AuxilaryClassifiers { get; } = new();
        internal List<string> AuxilaryDataKeys { get; } = new();
        internal List<char[]> AuxilaryDataBrackets { get; } = new();
        internal Dictionary<string, Dictionary<string, string>> StatusTransformations { get; } = new();
        internal Dictionary<string, HashSet<string>> ValidPrimary { get; } = new();
        internal Dictionary<string, HashSet<string>> ValidAuxilary { get; } = new();
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string Separator { get; set; }

        internal bool IsPrefixInitialized { get; set; } = false;
        internal bool IsPrimaryInitialized { get; set; } = false;
        internal bool IsAuxilaryDataInitialized { get; set; } = false;
        internal bool IsSecondaryInitialized { get; set; } = false;
        internal bool IsStatusInitialized { get; set; } = false;
        internal bool InitializedWithErrors { get; set; } = false;
        internal bool IsValid => IsPrefixInitialized && IsPrimaryInitialized && IsSecondaryInitialized && IsStatusInitialized;
        internal List<string> ErrorMessages { get; } = new();


        public LayerInfo GetLayerInfo(string layerName)
        {
            LayerInfo layerInfo = new(this);
            string[] decomposition = layerName.Split(Separator);

            int pointer = 0;
            // Вызываем обрабатывающие методы из очереди
            foreach (Func<string[], LayerInfo, int, int> func in ProcessingQueue)
            {
                pointer = func(decomposition, layerInfo, pointer);
            }
            return layerInfo;
        }

        public int ProcessPrefix(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Проверяем совпадение префикса (вообще по идее сюда попасть не должно)
            if (str[pointer] != layerInfo.ParentParser.Prefix)
                throw new WrongLayerException("Не совпадает префикс");
            // Назначаем префикс
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
            return pointer;
        }

        public int ProcessAuxilaryData(string[] str, LayerInfo layerInfo, int pointer)
        {
            for (int i = 0; i < AuxilaryDataBrackets.Count; i++)
            {
                // Проверить текущую строку массива на начало с нужной скобки, если нет, проверить следующий классификатор
                if (!str[pointer].StartsWith(AuxilaryDataBrackets[i][0]))
                {
                    layerInfo.AuxilaryData[AuxilaryDataKeys[i]] = null;
                    continue;
                }
                // Если да - установить счётчик элементов на 1 и увеличивать пока не будет найдена закрывающая скобка
                int elementsCounter = 1;
                while (!str[pointer + elementsCounter - 1].EndsWith(AuxilaryDataBrackets[i][1]))
                {
                    elementsCounter++;
                }
                // Объединить строки, переместить указатель вперёд на число полученных элементов
                string auxData = string.Join(Separator, str.Skip(pointer - 1).Take(elementsCounter).ToArray());
                string formattedAuxData = auxData.Replace(AuxilaryDataBrackets[i][0].ToString(), "").Replace(AuxilaryDataBrackets[i][1].ToString(), "");
                layerInfo.AuxilaryData[AuxilaryDataKeys[i]] = formattedAuxData;
                pointer += elementsCounter;
            }
            return pointer;
        }

        public int ProcessSecondaryClassifiers(string[] str, LayerInfo layerInfo, int pointer)
        {
            // Установить счётчик элементов на 0 и увеличивать пока не будет найден элемент, содержащий статус,
            // затем передать значения и переместить указатель вперёд
            int elementsCounter = 0;
            while (!StatusClassifiers.ContainsKey(str[pointer + elementsCounter]))
            {
                elementsCounter++;
                if (pointer + elementsCounter > str.Length)
                    throw new WrongLayerException("Не найден статус");
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
}
