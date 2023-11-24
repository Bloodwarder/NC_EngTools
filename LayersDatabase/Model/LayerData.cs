using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayersDatabase.Model
{
    public class LayerData
    {

        public string Name { get; set; }

        /// <summary>
        /// Глобальная ширина
        /// </summary>
        public double ConstantWidth { get; set; }
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        public double LTScale { get; set; }
        /// <summary>
        /// Красный
        /// </summary>
        public byte Red { get; set; }
        /// <summary>
        /// Зелёный
        /// </summary>
        public byte Green { get; set; }
        /// <summary>
        /// Синий
        /// </summary>
        public byte Blue { get; set; }
        /// <summary>
        /// Тип линий
        /// </summary>
        public string LineTypeName { get; set; }
        /// <summary>
        /// Вес линий
        /// </summary>
        public int LineWeight { get; set; }
        /// <summary>
        /// Имя шаблона
        /// </summary>
        public string DrawTemplate { get; set; }
        /// <summary>
        /// Символ для вставки посередине линии
        /// </summary>
        public string MarkChar { get; set; }
        /// <summary>
        /// Ширина (для прямоугольных шаблонов)
        /// </summary>
        public string Width { get; set; }
        /// <summary>
        /// Высота (для прямоугольных шаблонов)
        /// </summary>
        public string Height { get; set; }
        /// <summary>
        /// Дополнительная яркость внутреннего прямоугольника  (от - 1 до 1)
        /// </summary>
        public double InnerBorderBrightness { get; set; }
        /// <summary>
        /// Имя образца внутренней штриховки
        /// </summary>
        public string InnerHatchPattern { get; set; }
        /// <summary>
        /// Масштаб внутренней штриховки
        /// </summary>
        public double InnerHatchScale { get; set; }
        /// <summary>
        /// Дополнительная яркость внутренней штриховки  (от - 1 до 1)
        /// </summary>
        public double InnerHatchBrightness { get; set; }
        /// <summary>
        /// Угол поворота внутренней штриховки
        /// </summary>
        public double InnerHatchAngle { get; set; }
        /// <summary>
        /// Ширина внешнего прямоугольника
        /// </summary>
        public string FenceWidth { get; set; }
        /// <summary>
        /// Высота внешнего прямоугольника
        /// </summary>
        public string FenceHeight { get; set; }
        /// <summary>
        /// Слой внешнего прямоугольника (если отличается от основного слоя)
        /// </summary>
        public string FenceLayer { get; set; }
        /// <summary>
        /// Имя образца внешней штриховки
        /// </summary>
        public string OuterHatchPattern { get; set; }
        /// <summary>
        /// Масштаб внешней штриховки
        /// </summary>
        public double OuterHatchScale { get; set; }
        /// <summary>
        /// Дополнительная яркость внешней штриховки (от - 1 до 1)
        /// </summary>
        public double OuterHatchBrightness { get; set; }
        /// <summary>
        /// Угол поворота внешней штриховки
        /// </summary>
        public double OuterHatchAngle { get; set; }
        /// <summary>
        /// Радиус
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Имя блока
        /// </summary>
        public string BlockName { get; set; }
        /// <summary>
        /// Смещение точки вставки блока по оси X
        /// </summary>
        public double BlockXOffset { get; set; }
        /// <summary>
        /// Смещение точки вставки блока по оси Y
        /// </summary>
        public double BlockYOffset { get; set; }
        /// <summary>
        /// Путь к файлу для импорта блока
        /// </summary>
        public string BlockPath { get; set; }
    }
}
