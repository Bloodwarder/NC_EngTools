using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NameClassifiers.Filters
{
    [XmlRoot("LegendFilters")]
    public class GlobalFilters
    {
        GlobalFilters() { }
        [XmlAttribute("DefaultLabel")]
        public string DefaultLabel { get; set; }
        [XmlArray("")]
        //[XmlArrayItem(ElementName="FilterSection", Type=typeof(LegendFilterSection))]
        public LegendFilterSection[] Sections { get; set;}
    }

}
