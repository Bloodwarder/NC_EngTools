using NameClassifiers.References;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot(ElementName = "Grid")]
    public class GridFilter : ReferenceCollectionFilter, ICloneable
    {
        public GridFilter()
        {

        }
        [XmlAttribute("Label")]
        public string? Label { get; set; }

        //[XmlElement(Type = typeof(SectionReference))]
        //[XmlElement(Type = typeof(StatusReference))]
        //[XmlElement(Type = typeof(ChapterReference))]
        //[XmlElement(Type = typeof(ClassifierReference))]
        //[XmlElement(Type = typeof(DataReference))]
        //[XmlElement(Type = typeof(BoolReference))]
        //public List<SectionReference> References { get; set; } = new();

        [XmlElement(nameof(DistinctMode), IsNullable = false)]
        public DistinctMode? DistinctMode { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public bool Validate() => !References.Any(r => r.GetType() == DistinctMode?.Reference.GetType());

    }
}
