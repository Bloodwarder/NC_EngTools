using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersIO.Model
{
    /// <summary>
    /// Собственный тип LayerData, содержащий стандартные свойства объектов
    /// </summary>
    public class LayerPropertiesData
    {
        /// <summary>
        /// Глобальная ширина
        /// </summary>
        [Required]
        public double ConstantWidth { get; set; }
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        [Required]
        public double LinetypeScale { get; set; }
        /// <summary>
        /// Красный
        /// </summary>
        [Required]
        public byte Red { get; set; }
        /// <summary>
        /// Зелёный
        /// </summary>
        [Required]
        public byte Green { get; set; }
        /// <summary>
        /// Синий
        /// </summary>
        [Required]
        public byte Blue { get; set; }
        /// <summary>
        /// Тип линий
        /// </summary>
        [Required]
        public string LinetypeName { get; set; }
        /// <summary>
        /// Вес линий
        /// </summary>
        [Required]
        public int LineWeight { get; set; }
    }
}
