using System.Text;

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
        public bool SuffixTagged { get; set; } = false;

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



        public void SwitchStatus(string newStatus)
        {
            if (ParentParser.Status.ValidateString(newStatus))
                Status = newStatus;
            else
                throw new Exception("Неверный статус");
        }
        public void ChangeAuxilaryData(string key, string value)
        {
            if (!ParentParser.AuxilaryData[key].Validation?.TryValidateAndTransform(this) ?? true)
                throw new Exception($"Нельзя назначить {key} для указанного объекта");
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

        private string GetNameByType(NameType nameType)
        {
            List<string> members = new();
            ParentParser.Processor!.ComposeName(members, this, nameType);
            string sep = ParentParser.Separator;
            return string.Join(sep, members);
        }
    }
}
