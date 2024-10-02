using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NameClassifiers.References
{

    public class Transformation
    {
        [XmlArray(nameof(Source))]
        [XmlArrayItem(Type = typeof(ChapterReference))]
        [XmlArrayItem(Type = typeof(ClassifierReference))]
        [XmlArrayItem(Type = typeof(DataReference))]
        [XmlArrayItem(Type = typeof(StatusReference))]
        [XmlArrayItem(Type = typeof(BoolReference))]
        public SectionReference[] Source { get; set; }

        [XmlArray(nameof(Output))]
        [XmlArrayItem(Type = typeof(ChapterReference))]
        [XmlArrayItem(Type = typeof(ClassifierReference))]
        [XmlArrayItem(Type = typeof(DataReference))]
        [XmlArrayItem(Type = typeof(StatusReference))]
        [XmlArrayItem(Type = typeof(BoolReference))]
        public SectionReference[] Output { get; set; }
    }
}
