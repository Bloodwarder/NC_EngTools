using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot(ElementName = "Grid")]
    public class GridFilter
    {
        public GridFilter()
        {

        }
        [XmlAttribute("Name")]
        public string? Name { get; set; }

        [XmlArray("")]
        [XmlArrayItem(Type = typeof(SectionReference)),
            XmlArrayItem(Type = typeof(StatusReference)),
            XmlArrayItem(Type = typeof(ChapterReference)),
            XmlArrayItem(Type = typeof(ClassifierReference)),
            XmlArrayItem(Type = typeof(DataReference))]
        public SectionReference[] Sections { get; set; }

        [XmlElement(nameof(DistinctMode))]
        public DistinctMode DistinctMode { get; set;}
    }
}
