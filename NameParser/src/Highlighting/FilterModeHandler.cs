using NameClassifiers.References;

using System.Xml.Serialization;

namespace NameClassifiers.Highlighting
{

    public class FilterModeHandler : ReferenceCollectionFilter
    {
        public FilterModeHandler() { }

        [XmlElement(ElementName = "Property", Type = typeof(SinglePropertyHandler))]
        public SinglePropertyHandler[]? Assignations { get; set; }
    }



}
