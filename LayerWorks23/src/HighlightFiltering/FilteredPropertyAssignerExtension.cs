using LayerWorks.LayerProcessing;
using LoaderCore.Utilities;
using NameClassifiers.Highlighting;

namespace LayerWorks.HighlightFiltering
{
    internal static class FilteredPropertyAssignerExtension
    {
        public static Action<IEnumerable<VisualizerLayerWrapper>> GetAction(this FilteredPropertyAssigner assigner, PropertyBinder<StatedLayerProps> binder)
        {
            StatedLayerProps props = new();
            binder.SetProperty(props, assigner.Value);
            var predicate = assigner.GetPredicate();
            return wrappers => wrappers.Where(w => predicate(w.LayerInfo)).ToList().ForEach(w => w.Push(props));
        }
    }

}
