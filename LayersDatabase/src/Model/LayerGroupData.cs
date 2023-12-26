using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    public class LayerGroupData
    {

        public int Id { get; set; }

        public string MainName { get; set; }

        public LayerLegendData LayerLegendData { get; set; }

        //public int? AlternateLayerId { get; set; }
        //public LayerGroupData? AlternateLayer { get; set; }

        public List<LayerData> Layers { get; } = new();

    }
}
