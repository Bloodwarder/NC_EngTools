using System.Xml.Serialization;

namespace NameClassifiers.References
{
    public class ReferenceCollectionFilter
    {
        [XmlIgnore]
        private protected SectionReference[][]? _presortedReferences;
#nullable disable warnings
        public ReferenceCollectionFilter() { }
#nullable restore

        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        [XmlElement(Type = typeof(BoolReference))]
        public SectionReference[]? References { get; set; } = null!;
        public virtual Func<LayerInfo, bool> GetPredicate()
        {
            // Если нечего проверять возвращаем предикат без фильтра
            if (References == null || References.Length == 0)
                return info => true;
            _presortedReferences ??= PreSortReferences(References);
            // Должно выполняться хотя бы одно из условий секций каждого типа
            return SectionReference.GetPredicate(_presortedReferences!);
        }

        private protected static SectionReference[][] PreSortReferences(IEnumerable<SectionReference> references)
        {
            return SectionReference.SortByTypes(references);
        }
    }
}
