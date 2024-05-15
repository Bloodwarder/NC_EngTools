using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("DistinctMode")]
    public class DistinctMode
    {
        public DistinctMode() { }

        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        [XmlElement(Type = typeof(StatusReference))]
        public SectionReference[] References { get; set; }
    }
}
