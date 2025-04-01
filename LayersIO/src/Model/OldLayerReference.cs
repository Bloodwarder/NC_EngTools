using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    public class OldLayerReference
    {
        public OldLayerReference() { }
        public string OldLayerGroupName { get; set; }
        public int NewLayerGroupId { get; set; }
        public LayerGroupData NewLayerGroup { get; set; }
    }
}
