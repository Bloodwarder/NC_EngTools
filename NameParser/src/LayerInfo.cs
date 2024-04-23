using System.Text;

namespace NameClassifiers
{
    public class LayerInfo
    {
        public NameParser ParentParser { get; }

        public string Prefix { get; internal set; }
        public string PrimaryClassifier { get; internal set; }
        public Dictionary<string, string> AuxilaryClassifiers { get; } = new();
        public Dictionary<string, string?> AuxilaryData { get; }
        public string SecondaryClassifiers { get; internal set; }
        public string Status { get; internal set; }
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
        public string Name
        {
            get
            {
                List<string> members = new();
                ParentParser.Processor!.ComposeName(members, this);
                string sep = ParentParser.Separator;
                return string.Join(sep, members);
            }
        }

        public LayerInfo(NameParser parser)
        {
            ParentParser = parser;
        }

        public void SwitchStatus(string newStatus)
        {
            if (ParentParser.Status.ValidateString(newStatus))
                Status = newStatus;
            else
                throw new Exception("Неверный статус");
        }
        public void ChangeAuxilaryData(string key, string value)
        {
            if (!ParentParser.ValidPrimary[key].Contains(PrimaryClassifier))
                throw new Exception($"Нельзя назначить {key} для объекта типа \"{PrimaryClassifier}\"");
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

        public void AlterSecondaryClassifier(string newMainName)
        {
            string[] decomp = newMainName.Split(ParentParser.Separator);
            int counter = 0;
            if (decomp[0] == PrimaryClassifier)
                counter++;
            counter += AuxilaryClassifiers.Count();
            SecondaryClassifiers = string.Join(ParentParser.Separator, decomp.Skip(counter));
        }
                
    }
}
