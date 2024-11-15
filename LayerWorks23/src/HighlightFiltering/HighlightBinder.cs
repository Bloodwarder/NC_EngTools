using LayerWorks.LayerProcessing;
using LoaderCore.Utilities;
using NameClassifiers.Highlighting;

namespace LayerWorks.HighlightFiltering
{
    internal class HighlightBinder
    {
        private readonly PropertyBinder<StatedLayerProps> _binder;
        private readonly SinglePropertyHandler _handler;
        internal HighlightBinder(SinglePropertyHandler handler)
        {
            _handler = handler;
            _binder = PropertyBinder<StatedLayerProps>.Create(handler.PropertyName);
        }

        internal IEnumerable<Action<IEnumerable<VisualizerLayerWrapper>>> Actions =>
            _handler.FilteredPropertyAssigners.Select(fpa => fpa.GetAction(_binder));
    }
}
