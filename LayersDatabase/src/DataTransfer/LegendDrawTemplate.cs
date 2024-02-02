using LayersIO.Model;

namespace LayersIO.DataTransfer
{
    /// <summary>
    /// Данные для отрисовки шаблона легенды
    /// </summary>
    public class LegendDrawTemplate
    {
        /// <summary>
        /// Имя шаблона
        /// </summary>
        public string DrawTemplate;
        /// <summary>
        /// Символ для вставки посередине линии
        /// </summary>
        public string MarkChar;
        /// <summary>
        /// Ширина (для прямоугольных шаблонов)
        /// </summary>
        public string Width;
        /// <summary>
        /// Высота (для прямоугольных шаблонов)
        /// </summary>
        public string Height;
        /// <summary>
        /// Дополнительная яркость внутреннего прямоугольника  (от - 1 до 1)
        /// </summary>
        public double InnerBorderBrightness;
        /// <summary>
        /// Имя образца внутренней штриховки
        /// </summary>
        public string InnerHatchPattern;
        /// <summary>
        /// Масштаб внутренней штриховки
        /// </summary>
        public double InnerHatchScale;
        /// <summary>
        /// Дополнительная яркость внутренней штриховки  (от - 1 до 1)
        /// </summary>
        public double InnerHatchBrightness;
        /// <summary>
        /// Угол поворота внутренней штриховки
        /// </summary>
        public double InnerHatchAngle;
        /// <summary>
        /// Ширина внешнего прямоугольника
        /// </summary>
        public string FenceWidth;
        /// <summary>
        /// Высота внешнего прямоугольника
        /// </summary>
        public string FenceHeight;
        /// <summary>
        /// Слой внешнего прямоугольника (если отличается от основного слоя)
        /// </summary>
        public string FenceLayer;
        /// <summary>
        /// Имя образца внешней штриховки
        /// </summary>
        public string OuterHatchPattern;
        /// <summary>
        /// Масштаб внешней штриховки
        /// </summary>
        public double OuterHatchScale;
        /// <summary>
        /// Дополнительная яркость внешней штриховки (от - 1 до 1)
        /// </summary>
        public double OuterHatchBrightness;
        /// <summary>
        /// Угол поворота внешней штриховки
        /// </summary>
        public double OuterHatchAngle;
        /// <summary>
        /// Радиус
        /// </summary>
        public double Radius;
        /// <summary>
        /// Имя блока
        /// </summary>
        public string BlockName;
        /// <summary>
        /// Смещение точки вставки блока по оси X
        /// </summary>
        public double BlockXOffset;
        /// <summary>
        /// Смещение точки вставки блока по оси Y
        /// </summary>
        public double BlockYOffset;
        /// <summary>
        /// Путь к файлу для импорта блока
        /// </summary>
        public string BlockPath;
    }
}
