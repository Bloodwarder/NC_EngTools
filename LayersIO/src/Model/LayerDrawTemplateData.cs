namespace LayersIO.Model
{
    /// <summary>
    /// Сбоственный тип LayerData, содержащий информацию об отрисовке в таблице условных обозначений
    /// </summary>
    public class LayerDrawTemplateData
    {
        /// <summary>
        /// Имя шаблона
        /// </summary>
        public string? DrawTemplate { get; set; }
        /// <summary>
        /// Символ для вставки посередине линии
        /// </summary>
        public string? MarkChar { get; set; }
        /// <summary>
        /// Ширина (для прямоугольных шаблонов)
        /// </summary>
        public string? Width { get; set; } = "1*";
        /// <summary>
        /// Высота (для прямоугольных шаблонов)
        /// </summary>
        public string? Height { get; set; } = "1*";
        /// <summary>
        /// Дополнительная яркость внутреннего прямоугольника  (от - 1 до 1)
        /// </summary>
        public double? InnerBorderBrightness { get; set; }
        /// <summary>
        /// Имя образца внутренней штриховки
        /// </summary>
        public string? InnerHatchPattern { get; set; } = "SOLID";
        /// <summary>
        /// Масштаб внутренней штриховки
        /// </summary>
        public double? InnerHatchScale { get; set; } = 1d;
        /// <summary>
        /// Дополнительная яркость внутренней штриховки  (от - 1 до 1)
        /// </summary>
        public double? InnerHatchBrightness { get; set; } = 0;
        /// <summary>
        /// Угол поворота внутренней штриховки
        /// </summary>
        public double? InnerHatchAngle { get; set; } = 0;
        /// <summary>
        /// Ширина внешнего прямоугольника
        /// </summary>
        public string? FenceWidth { get; set; } = "1*";
        /// <summary>
        /// Высота внешнего прямоугольника
        /// </summary>
        public string? FenceHeight { get; set; } = "1*";
        /// <summary>
        /// Слой внешнего прямоугольника (если отличается от основного слоя)
        /// </summary>
        public string? FenceLayer { get; set; }
        /// <summary>
        /// Имя образца внешней штриховки
        /// </summary>
        public string? OuterHatchPattern { get; set; }
        /// <summary>
        /// Масштаб внешней штриховки
        /// </summary>
        public double? OuterHatchScale { get; set; } = 1d;
        /// <summary>
        /// Дополнительная яркость внешней штриховки (от - 1 до 1)
        /// </summary>
        public double? OuterHatchBrightness { get; set; } = 0;
        /// <summary>
        /// Угол поворота внешней штриховки
        /// </summary>
        public double? OuterHatchAngle { get; set; } = 0;
        /// <summary>
        /// Радиус
        /// </summary>
        public double? Radius { get; set; } = 10;
        /// <summary>
        /// Имя блока
        /// </summary>
        public string? BlockName { get; set; }
        /// <summary>
        /// Смещение точки вставки блока по оси X
        /// </summary>
        public double? BlockXOffset { get; set; } = 0;
        /// <summary>
        /// Смещение точки вставки блока по оси Y
        /// </summary>
        public double? BlockYOffset { get; set; } = 0;
        /// <summary>
        /// Путь к файлу для импорта блока
        /// </summary>
        public string? BlockPath { get; set; }

    }
}
