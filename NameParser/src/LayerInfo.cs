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
            BooleanSection[] sections = SuffixTagged.Keys.Select(k => (BooleanSection)ParentParser.GetSection<BooleanSection>(k)).ToArray();
            if (sections == null)
                return;
            foreach (var section in sections) 
            {
                if (section.Validators == null || section.Validators.Count < 1)
                    continue;
                if (!section.Validators?.All(v => v.ValidateLayerInfo(this)) ?? false)
                    SuffixTagged[section.Name] = false;
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
            // TODO: Изменить логику так, чтобы не предполагалось, что статус стоит в конце, или сделать возможным получить LayerInfo без статуса
            string statusStub = ParentParser.Status.GetDescriptionDictionary().Keys.First(); // затычка, чтобы объект парсился
            var alterLayerInfo = ParentParser.GetLayerInfo($"{Prefix}{ParentParser.Separator}{newMainName}{ParentParser.Separator}{statusStub}");
            SecondaryClassifiers = alterLayerInfo.SecondaryClassifiers;
        }

        public void SwitchSuffix(string key, bool value)
        {
            var validators = ParentParser.GetSection<BooleanSection>(key).Validators;
            if (validators != null)
            {
                bool isValid = validators.TrueForAll(v => v.ValidateLayerInfo(this));
                if (!isValid)
                {
                    bool isAssingedValid = validators.TrueForAll(v => v.Transform(this));
                    if (!isAssingedValid)
                        throw new WrongLayerException($"Нельзя назначить суффикс с тегом \"{key}\" для указанного объекта");
                }
            }
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
