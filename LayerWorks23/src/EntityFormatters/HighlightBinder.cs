using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Utilities;
using static NanocadUtilities.EditorHelper;
using LogicExtensions;
using NameClassifiers;
using NameClassifiers.Highlighting;
using NameClassifiers.References;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;

namespace LayerWorks.EntityFormatters
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

    internal class HighlightBinder
    {
        PropertyBinder<StatedLayerProps> _binder;
        SinglePropertyHandler _handler;
        internal HighlightBinder(SinglePropertyHandler handler)
        {
            _handler = handler;
            _binder = PropertyBinder<StatedLayerProps>.Create(handler.PropertyName);
        }

        public IEnumerable<Action<IEnumerable<VisualizerLayerWrapper>>> Actions =>
            _handler.FilteredPropertyAssigners.Select(fpa => fpa.GetAction(_binder));

    }

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

    internal static class HighlightFilterExtension
    {
        public static void Method(this HighlightFilter filter, IEnumerable<VisualizerLayerWrapper> wrappers)
        {
            StatedLayerProps propsDisable = new() { IsOff = true };
            StatedLayerProps propsEnable = new() { IsOff = false };
            bool disableEmpty = filter.Disable == null;
            bool enableEmpty = filter.Enable == null;
            Action<VisualizerLayerWrapper> actionDisable = lw => lw.Push(propsDisable);
            Action<VisualizerLayerWrapper> actionEnable = lw => lw.Push(propsEnable);
            if (filter.Highlight != null)
            {
                ProcessFilterModeHandler(filter.Highlight, wrappers);
            }

            if (disableEmpty && !enableEmpty) 
            {
                ProcessFilterModeHandler(filter.Enable!, wrappers, actionEnable, actionDisable);
            }
            else if (!disableEmpty && enableEmpty)
            {
                ProcessFilterModeHandler(filter.Disable!, wrappers, actionDisable, actionEnable);
            }
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
                string keyword = GetStringKeywordResult(keywords!, descriptions!, "Выберите раздел для фильтрации подсветки");
                filteredWrappers = handler.FilterByKeyword(wrappers, keyword);
            }
            filteredWrappers ??= wrappers;

            if (!actions.Any())
            {
                handler.ApplyWithDefaultFilter(wrappers);
            }
            else if (actions.Length == 1)
            {
                handler.ApplyCustomAction(wrappers, actions[0]);
            }
            else if (actions.Length == 2)
            {
                handler.ApplyCustomActionsSplitted(wrappers, actions[0], actions[1]);
            }
        }
    }

}
