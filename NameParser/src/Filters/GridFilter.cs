using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot(ElementName = "Grid")]
    public class GridFilter : ICloneable
    {
        [XmlIgnore]
        SectionReference[][]? _sectionsMatchCheck;
        public GridFilter()
        {

        }
        [XmlAttribute("Label")]
        public string? Label { get; set; }

        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        [XmlElement(Type = typeof(BoolReference))]
        public List<SectionReference> References { get; set; } = new();

        [XmlElement(nameof(DistinctMode), IsNullable = false)]
        public DistinctMode? DistinctMode { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public bool Validate() => !References.Any(r => r.GetType() == DistinctMode?.Reference.GetType());

        public Func<LayerInfo, bool> GetGridPredicate()
        {
            if (References.Count == 0)
                // Нечего проверять
                return info => true;
            if (_sectionsMatchCheck == null)
                InitializeSectionCheck();
            // Должно выполняться хотя бы одно из условий секций каждого типа
            return info => _sectionsMatchCheck!.All(rArr => rArr.Any(r => r.Match(info)));
        }

        private void InitializeSectionCheck()
        {
            // Разбить секции на отдельные массивы по типу;
            Type[] types = References.Select(r => r.GetType()).Distinct().ToArray();
            _sectionsMatchCheck = types.Select(t => References.Where(r => r.GetType() == t).ToArray()).ToArray();
        }
    }
}
