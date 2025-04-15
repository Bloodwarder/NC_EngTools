using LayersIO.Connection;
using LayersIO.Model;
using Microsoft.EntityFrameworkCore;

namespace LayersDatabaseEditor.ViewModel.Ordering
{
    public class LayerOrderedVm : OrderedItemViewModel<LayerData>
    {
        public LayerOrderedVm(LayerData item)
            : base(item, i => i.Name, i => i.LayerPropertiesData.DrawOrderIndex, (i, index) => i.LayerPropertiesData.DrawOrderIndex = index)
        { }
    }
}
