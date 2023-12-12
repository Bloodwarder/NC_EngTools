using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    public class LayerLegendData
    {
        public int Id { get; set; }

        public int LayerGroupDataId { get; set; }
        public LayerGroupData LayerGroupData { get; set; }


        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank;
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label;
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string? SubLabel;
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer;
    }
}
