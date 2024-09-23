using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    /// <summary>
    /// Входит в фильтр сетки для сборки легенд.
    /// Обозначает, что для каждого вхождения соответствующего классификатора необходимо создавать отдельную сетку.
    /// </summary>
    [XmlRoot("DistinctMode")]
    public class DistinctMode
    {
        public DistinctMode() { }

        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(BoolReference))]
        public SectionReference Reference { get; set; }
    }
}
