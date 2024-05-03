using System.Collections.Generic;

namespace LayerWorks.Legend
{
    internal class LegendGridRowComparer : IComparer<LegendGridRow>
    {
        public int Compare(LegendGridRow? x, LegendGridRow? y)
        {
            int xNum = x?.LegendData?.Rank ?? int.MaxValue;
            int yNum = y?.LegendData?.Rank ?? int.MaxValue;
            return xNum.CompareTo(yNum);
        }
    }
}
