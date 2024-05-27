using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Utilities;
using NameClassifiers;
using NameClassifiers.Highlighting;
using NameClassifiers.References;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LayerWorks.EntityFormatters
{
    internal class FilterModeHandlerExtension
    {
        
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
}
