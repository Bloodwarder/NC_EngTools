using LayerWorks.LayerProcessing;
using NameClassifiers.Highlighting;
using NameClassifiers.References;

namespace LayerWorks.HighlightFiltering
{
    internal static class FilterModeHandlerExtension
    {
        internal static bool IsChoiceNeeded(this FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers, out string[]? keywords, out string[]? descriptions)
        {
            if (handler.References?.Length == 1 && handler.References[0].Value == null)
            {
                SectionReference section = handler.References[0];
                var infos = wrappers.Select(w => w.LayerInfo);
                section.ExtractDistinctInfo(infos, out keywords, out Func<string, string> descriptionFunc);
                descriptions = keywords.Select(k => descriptionFunc(k)).ToArray();
                return true;
            }
            keywords = null;
            descriptions = null;
            return false;
        }

        internal static IEnumerable<VisualizerLayerWrapper> FilterByKeyword(this FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers, string keyword)
        {
            SectionReference clonedSection = (SectionReference)handler.References![0].Clone();
            clonedSection.Value = keyword;
            var filteredWrappers = wrappers.Where(w => clonedSection.Match(w.LayerInfo));
            return filteredWrappers;
        }

        internal static void ApplyWithDefaultFilter(this FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers)
        {
            var predicate = handler.GetPredicate();
            var filteredWrappers = wrappers.Where(w => predicate(w.LayerInfo));
            handler.ApplyUnfiltered(filteredWrappers);
        }

        internal static void ApplyUnfiltered(this FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers)
        {
            Action<IEnumerable<VisualizerLayerWrapper>>[] actions = handler.Assignations!.Select(a => new HighlightBinder(a)).SelectMany(b => b.Actions).ToArray();
            foreach (var action in actions)
            {
                action.Invoke(wrappers);
            }
        }

        internal static void ApplyCustomAction(this FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers, Action<VisualizerLayerWrapper> action)
        {
            var predicate = handler.GetPredicate();
            var filteredWrappers = wrappers.Where(w => predicate(w.LayerInfo));
            foreach (var wrapper in filteredWrappers)
            {
                action.Invoke(wrapper);
            }
        }
        internal static void ApplyCustomActionsSplitted(this FilterModeHandler handler,
                                                        IEnumerable<VisualizerLayerWrapper> wrappers,
                                                        Action<VisualizerLayerWrapper> actionTrue,
                                                        Action<VisualizerLayerWrapper> actionFalse)
        {
            var predicate = handler.GetPredicate();
            foreach (var wrapper in wrappers)
            {
                if (predicate(wrapper.LayerInfo))
                    actionTrue.Invoke(wrapper);
                else
                    actionFalse.Invoke(wrapper);
            }

        }
    }

}
