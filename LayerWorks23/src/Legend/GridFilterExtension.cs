using NameClassifiers;
using NameClassifiers.Filters;
using NameClassifiers.References;

namespace LayerWorks.Legend
{
    internal static class GridFilterExtension
    {
        internal static IEnumerable<GridFilter> CheckAndUnfoldDistinctMode(this GridFilter filter)
        {
            if (filter.DistinctMode == null)
            {
                yield return filter;
                yield break;
            }
            IEnumerable<LayerInfo> info = GridsComposer.ActiveComposer!.SourceCells.Select(cell => cell.Layer.LayerInfo);

            switch (filter.DistinctMode.Reference)
            {
                case ChapterReference:
                    string[] primaries = info.Select(i => i.PrimaryClassifier!).Distinct().ToArray();
                    foreach (var grid in CreateExactGrids(filter, primaries))
                        yield return grid;
                    break;
                case StatusReference:
                    string[] statuses = info.Select(i => i.Status!).Distinct().ToArray();
                    foreach (var grid in CreateExactGrids(filter, statuses))
                        yield return grid;
                    break;
                case ClassifierReference clRef:
                    string[] classifiers = info.Select(i => i.AuxilaryClassifiers[clRef.Name]!).Distinct().ToArray();
                    foreach (var grid in CreateExactGrids(filter, classifiers))
                        yield return grid;
                    break;
                case DataReference dRef:
                    string[] data = info.Select(i => i.AuxilaryData[dRef.Name]!).Distinct().ToArray();
                    foreach (var grid in CreateExactGrids(filter, data))
                        yield return grid;
                    break;

            }
        }

        private static IEnumerable<GridFilter> CreateExactGrids(GridFilter filter, IEnumerable<string> values)
        {
            foreach (var element in values)
            {
                GridFilter clone = (GridFilter)filter.Clone();
                clone.DistinctMode = null;
                SectionReference newRef = (SectionReference)filter.DistinctMode!.Reference.Clone();
                newRef.Value = element;
                clone.References = clone.References.Append(newRef).ToArray();
                clone.Label += $" ({element})";
                yield return clone;
            }
        }
    }
}
