using System.Collections.Generic;

namespace LayerWorks.Legend
{
    internal class LegendGridRowComparer : IComparer<LegendGridRow>
    {
        public int Compare(LegendGridRow x, LegendGridRow y)
        {
            if (x.LegendData.Rank > y.LegendData.Rank)
                return 1;
            if (x.LegendData.Rank < y.LegendData.Rank)
                return -1;
            else
                return 0;
        }
    }
}
