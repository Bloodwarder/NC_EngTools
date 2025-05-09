﻿using System.ComponentModel.DataAnnotations;

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
        public double LinetypeScale { get; set; } = 1d;
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
        public string LinetypeName { get; set; } = "Continuous";
        /// <summary>
        /// Вес линий
        /// </summary>
        [Required]
        public int LineWeight { get; set; } = -3;

        public int DrawOrderIndex { get; set; }
    }
}
