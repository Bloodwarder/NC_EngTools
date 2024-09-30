using NameClassifiers.Sections;

namespace NameClassifiers
{
    public class LayerInfo
    {
        public LayerInfo(NameParser parser)
        {
            ParentParser = parser;
        }
        public enum NameType
        {
            FullName,
            MainName,
            TrueName
        }
        public NameParser ParentParser { get; }
        public string? Prefix { get; internal set; }
        public string? PrimaryClassifier { get; internal set; }
        public Dictionary<string, string> AuxilaryClassifiers { get; } = new();
        public Dictionary<string, string?> AuxilaryData { get; } = new();
        public string? SecondaryClassifiers { get; internal set; }
        public string? Status { get; internal set; }
        public Dictionary<string, bool> SuffixTagged { get; private set; } = new();

        /// <summary>
        /// Значимое имя только с обязательными классификаторами без статуса (отражающее тип объекта)
        /// </summary>
        public string MainName => GetNameByType(NameType.MainName);
        /// <summary>
        /// Значимое имя только с обязательными классификаторами без префиксов
        /// </summary>
        public string TrueName => GetNameByType(NameType.TrueName);
        /// <summary>
        /// Полное имя со всей дополнительной информацией и классификаторами
        /// </summary>
        public string Name => GetNameByType(NameType.FullName);


        public bool IsValid => Prefix != null
                       && PrimaryClassifier != null
                       && SecondaryClassifiers != null
                       && Status != null;
        /// <summary>
        /// Изменить статус
        /// </summary>
        /// <param name="newStatus"></param>
        /// <exception cref="WrongLayerException"></exception>
        public void SwitchStatus(string newStatus)
        {
            if (ParentParser.Status.ValidateString(newStatus))
                Status = newStatus;
            else
                throw new WrongLayerException("Неверный статус");
            // если значение нового статуса не является корректным для имеющейся дополнительной информации - сбросить информацию
            foreach (var aux in ParentParser.AuxilaryData.Values)
            {
                var validation = aux.Validators;
                if (validation == null)
                    continue;
                if (!validation?.Any(v => v.ValidateLayerInfo(this)) ?? false)
                    AuxilaryData[aux.Name] = null;
            }
        }
        public void ChangeAuxilaryData(string key, string? value)
        {
            var validators = ParentParser.AuxilaryData[key].Validators;
            if (validators != null)
            {
                bool isValid = validators.TrueForAll(v => v.ValidateLayerInfo(this));
                if (!isValid)
                {
                    bool isAssingedValid = validators.TrueForAll(v => v.Transform(this)); 
                    if (!isAssingedValid)
                        throw new WrongLayerException($"Нельзя назначить {key} для указанного объекта");
                }
            }
            AuxilaryData[key] = value;
        }

        public void AlterSecondaryClassifier(string newMainName)
        {
            string[] decomp = newMainName.Split(ParentParser.Separator);
            int counter = 0;
            if (decomp[0] == PrimaryClassifier)
                counter++;
            counter += AuxilaryClassifiers.Count;
            SecondaryClassifiers = string.Join(ParentParser.Separator, decomp.Skip(counter));
        }

        public void SwitchSuffix(string key, bool value)
        {
            var section = (BooleanSection)ParentParser.GetSection<BooleanSection>(key);
            if (!section.Validation?.TryValidateAndTransform(this) ?? true)
                throw new WrongLayerException($"Нельзя назначить суффикс с тегом \"{key}\" для указанного объекта");
            SuffixTagged[key] = value;
        }
        /// <summary>
        /// Собрать строку с именем, с помощью цепочки секций парсера
        /// </summary>
        /// <param name="nameType">Тип имени</param>
        /// <returns>Строка с итоговым именем</returns>
        private string GetNameByType(NameType nameType)
        {
            List<string> members = new();
            ParentParser.Processor!.ComposeName(members, this, nameType);
            string sep = ParentParser.Separator;
            return string.Join(sep, members);
        }
    }
}
