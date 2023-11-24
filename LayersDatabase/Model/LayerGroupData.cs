using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    public class LayerGroupData
    {
        public string MainName { get; set; }

        public string MainNameAlter { get; set; }

        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string SubLabel { get; set; }
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer { get; set; }
    }
}
