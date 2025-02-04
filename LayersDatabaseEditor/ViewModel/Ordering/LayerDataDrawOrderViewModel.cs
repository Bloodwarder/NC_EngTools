using LayersIO.Model;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class LayerDataDrawOrderViewModel : OrderedItemViewModel<LayerData>
    {
        public LayerDataDrawOrderViewModel(LayerData item)
            : base(item, i => i.Name, i => i.LayerPropertiesData.DrawOrderIndex, (i, index) => i.LayerPropertiesData.DrawOrderIndex = index)
        { }
    }
}
