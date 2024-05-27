using System.Xml.Serialization;


namespace NameClassifiers.References
{

    [XmlInclude(typeof(ChapterReference))]
    [XmlInclude(typeof(StatusReference))]
    [XmlInclude(typeof(ClassifierReference))]
    [XmlInclude(typeof(DataReference))]
    [XmlInclude(typeof(BoolReference))]
    public abstract class SectionReference : ICloneable
    {
        [XmlAttribute("Value")]
        public string? Value { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
        /// <summary>
        /// Определяет соответствует ли объект LayerInfo критерию секции
        /// </summary>
        /// <param name="layerInfo">Проверяемый объект</param>
        /// <returns></returns>
        public abstract bool Match(LayerInfo layerInfo);

        /// <summary>
        /// Разбить секции на отдельные массивы по типу;
        /// </summary>
        /// <param name="sections">Коллекция секций</param>
        /// <returns></returns>
        public static SectionReference[][] SortByTypes(IEnumerable<SectionReference> sections)
        {
            Type[] types = sections.Select(r => r.GetType()).Distinct().ToArray();
            return types.Select(t => sections.Where(r => r.GetType() == t).ToArray()).ToArray();
        }

        /// <summary>
        /// Возвращает предикат для фильтрации по коллекции SectionReference
        /// </summary>
        /// <param name="sortedSections">секции, предварительно отсортированные по типу на отдельные массивы</param>
        /// <returns></returns>
        public static Func<LayerInfo, bool> GetPredicate(SectionReference[][] sortedSections)
        {
            // В каждом из массивов должен быть хотя бы один элемент
            if (!sortedSections.Any() || !sortedSections.All(sArr => sArr.Any()))
                // Нечего проверять
                return info => true;
            // Должно выполняться хотя бы одно из условий секций каждого типа
            return info => sortedSections!.All(rArr => rArr.Any(r => r.Match(info)));

        }
        public static Func<LayerInfo, bool> GetPredicate(IEnumerable<SectionReference> sections) => GetPredicate(SortByTypes(sections));

    }
}