using System.Xml.Serialization;
using NameClassifiers.Filters;

namespace LayerWorks.Legend
{
    [XmlRoot(ElementName ="Filter")]
    internal static class GridFilterExtension
    {
        internal static Func<LegendGridCell,bool> GetPredicate(this GridFilter filter)
        {
            throw new NotImplementedException();
        }
    }

}
