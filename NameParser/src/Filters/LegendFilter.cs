using NameClassifiers.Filters;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot(ElementName ="Filter")]
    public class LegendFilter
    {
        public LegendFilter()
        {  

        }
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlArray("")]
        [XmlArrayItem("Grid")]
        public GridFilter[] Grids { get; set; }
    }
}
