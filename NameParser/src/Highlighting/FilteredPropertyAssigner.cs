using NameClassifiers.References;
using NameClassifiers.SharedProperties;
using System.Xml.Serialization;

namespace NameClassifiers.Highlighting
{
    [XmlRoot("Assign")]
    public class FilteredPropertyAssigner : ReferenceCollectionFilter
    {
        public FilteredPropertyAssigner() { }

        [XmlElement(ElementName = "Value", Type = typeof(ValueContainer))]
        public ValueContainer Value { get; set; } = null!;

    }



}
