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

        [XmlElement(nameof(DistinctMode), IsNullable = false)]
        public DistinctMode? DistinctMode { get; set; }

        public object Clone() => this.MemberwiseClone();

        public bool Validate() => !References.Any(r => r.GetType() == DistinctMode?.Reference.GetType());
    }
}
