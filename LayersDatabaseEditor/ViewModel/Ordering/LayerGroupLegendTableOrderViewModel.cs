using LayersIO.Model;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class LayerGroupLegendTableOrderViewModel : OrderedItemViewModel<LayerGroupData>
    {
        public LayerGroupLegendTableOrderViewModel(LayerGroupData item)
            : base(item, i => i.Name, i => i.LayerLegendData.Rank, (i, index) => i.LayerLegendData.Rank = index)
        { }
    }
}
