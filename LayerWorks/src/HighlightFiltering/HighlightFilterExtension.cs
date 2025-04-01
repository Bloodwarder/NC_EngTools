using LayerWorks.LayerProcessing;
using NameClassifiers.Highlighting;
using static LoaderCore.NanocadUtilities.EditorHelper;

namespace LayerWorks.HighlightFiltering
{
    internal static class HighlightFilterExtension
    {
        public static void ApplyFilter(this HighlightFilter filter, IEnumerable<VisualizerLayerWrapper> wrappers)
        {
            StatedLayerProps propsDisable = new() { IsOff = true };
            StatedLayerProps propsEnable = new() { IsOff = false };
            bool disableEmpty = filter.Disable == null;
            bool enableEmpty = filter.Enable == null;
            Action<VisualizerLayerWrapper> actionDisable = lw => lw.Push(propsDisable);
            Action<VisualizerLayerWrapper> actionEnable = lw => lw.Push(propsEnable);
            if (filter.Highlight != null)
            {
                // Подсветить стандартным обработчиком
                ProcessFilterModeHandler(filter.Highlight, wrappers);
            }

            // XOR
            if (disableEmpty && !enableEmpty)
            {
                ProcessFilterModeHandler(filter.Enable!, wrappers, actionEnable, actionDisable);
            }
            else if (!disableEmpty && enableEmpty)
            {
                ProcessFilterModeHandler(filter.Disable!, wrappers, actionDisable, actionEnable);
            }
            // DEFAULT
            else if (!disableEmpty && !enableEmpty)
            {
                ProcessFilterModeHandler(filter.Enable!, wrappers, actionEnable);
                ProcessFilterModeHandler(filter.Disable!, wrappers, actionDisable);
            }
        }

        private static void ProcessFilterModeHandler(FilterModeHandler handler, IEnumerable<VisualizerLayerWrapper> wrappers, params Action<VisualizerLayerWrapper>[] actions)
        {
            bool needChoice = handler.IsChoiceNeeded(wrappers, out string[]? keywords, out string[]? descriptions);
            IEnumerable<VisualizerLayerWrapper>? filteredWrappers = null;
            if (needChoice)
            {
                string keyword = GetStringKeyword(keywords!, descriptions!, "Выберите раздел для фильтрации подсветки");
                filteredWrappers = handler.FilterByKeyword(wrappers, keyword);
            }
            filteredWrappers ??= wrappers;

            if (!actions.Any())
            {
                handler.ApplyWithDefaultFilter(filteredWrappers);
            }
            else if (actions.Length == 1)
            {
                handler.ApplyCustomAction(filteredWrappers, actions[0]);
            }
            else if (actions.Length == 2)
            {
                handler.ApplyCustomActionsSplitted(filteredWrappers, actions[0], actions[1]);
            }
        }
    }

}
