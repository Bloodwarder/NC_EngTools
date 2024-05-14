using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("DistinctMode")]
    public class DistinctMode
    {
        public DistinctMode() { }

        [XmlArrayItem(Type = typeof(SectionReference)),
            XmlArrayItem(Type = typeof(StatusReference)),
            XmlArrayItem(Type = typeof(ChapterReference)),
            XmlArrayItem(Type = typeof(ClassifierReference)),
            XmlArrayItem(Type = typeof(DataReference))]
        public SectionReference[] References { get; set; }
    }
}
