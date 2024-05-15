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
        [XmlAttribute("Label")]
        public string? Label { get; set; }

        [XmlElement(Type = typeof(SectionReference))]
        [XmlElement(Type = typeof(StatusReference))]
        [XmlElement(Type = typeof(ChapterReference))]
        [XmlElement(Type = typeof(ClassifierReference))]
        [XmlElement(Type = typeof(DataReference))]
        public SectionReference[] Sections { get; set; }

        [XmlElement(nameof(DistinctMode))]
        public DistinctMode DistinctMode { get; set; }
    }
}
