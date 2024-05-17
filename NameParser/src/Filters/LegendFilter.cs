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
        public LegendFilter(bool defaultFilter) : base()
        {
            if (defaultFilter)
            {
                Name = "Общий";
                Grids = new GridFilter[] { new() };
            }
        }
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlElement("Grid", Type = typeof(GridFilter))]
        public GridFilter[] Grids { get; set; }
    }
}
