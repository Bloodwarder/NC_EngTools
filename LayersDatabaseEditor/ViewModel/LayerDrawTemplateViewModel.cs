using LayersIO.DataTransfer;
using LayersIO.Model;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerDrawTemplateViewModel
    {
        LayerDrawTemplateData _layerDrawTemplateData;
        public LayerDrawTemplateViewModel(LayerDrawTemplateData layerDrawTemplateData)
        {
            _layerDrawTemplateData = layerDrawTemplateData;
            DrawTemplate = Enum.Parse<DrawTemplate>(_layerDrawTemplateData.DrawTemplate ?? "Undefined");
            MarkChar = layerDrawTemplateData.MarkChar;
            Width = layerDrawTemplateData.Width;
            Height = layerDrawTemplateData.Height;
            InnerBorderBrightness = layerDrawTemplateData.InnerBorderBrightness;
            InnerHatchPattern = layerDrawTemplateData.InnerHatchPattern;
            InnerHatchScale = layerDrawTemplateData.InnerHatchScale;
            InnerHatchBrightness = layerDrawTemplateData.InnerHatchBrightness;
            InnerHatchAngle = layerDrawTemplateData.InnerHatchAngle;
            FenceWidth = layerDrawTemplateData.FenceWidth;
            FenceHeight = layerDrawTemplateData.FenceHeight;
            FenceLayer = layerDrawTemplateData.FenceLayer;
            OuterHatchPattern = layerDrawTemplateData.OuterHatchPattern;
            OuterHatchScale = layerDrawTemplateData.OuterHatchScale;
            OuterHatchBrightness = layerDrawTemplateData.OuterHatchBrightness;
            OuterHatchAngle = layerDrawTemplateData.OuterHatchAngle;
            Radius = layerDrawTemplateData.Radius;
            BlockName = layerDrawTemplateData.BlockName;
            BlockXOffset = layerDrawTemplateData.BlockXOffset;
            BlockYOffset = layerDrawTemplateData.BlockYOffset;
            BlockPath = layerDrawTemplateData.BlockPath;
        }

        /// <summary>
        /// Имя шаблона
        /// </summary>
        public DrawTemplate? DrawTemplate { get; set; }
        /// <summary>
        /// Символ для вставки посередине линии
        /// </summary>
        public string? MarkChar { get; set; }
        /// <summary>
        /// Ширина (для прямоугольных шаблонов)
        /// </summary>
        public string? Width { get; set; }
        /// <summary>
        /// Высота (для прямоугольных шаблонов)
        /// </summary>
        public string? Height { get; set; }
        /// <summary>
        /// Дополнительная яркость внутреннего прямоугольника  (от - 1 до 1)
        /// </summary>
        public double? InnerBorderBrightness { get; set; }
        /// <summary>
        /// Имя образца внутренней штриховки
        /// </summary>
        public string? InnerHatchPattern { get; set; }
        /// <summary>
        /// Масштаб внутренней штриховки
        /// </summary>
        public double? InnerHatchScale { get; set; }
        /// <summary>
        /// Дополнительная яркость внутренней штриховки  (от - 1 до 1)
        /// </summary>
        public double? InnerHatchBrightness { get; set; }
        /// <summary>
        /// Угол поворота внутренней штриховки
        /// </summary>
        public double? InnerHatchAngle { get; set; }
        /// <summary>
        /// Ширина внешнего прямоугольника
        /// </summary>
        public string? FenceWidth { get; set; }
        /// <summary>
        /// Высота внешнего прямоугольника
        /// </summary>
        public string? FenceHeight { get; set; }
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
        public double? OuterHatchScale { get; set; }
        /// <summary>
        /// Дополнительная яркость внешней штриховки (от - 1 до 1)
        /// </summary>
        public double? OuterHatchBrightness { get; set; }
        /// <summary>
        /// Угол поворота внешней штриховки
        /// </summary>
        public double? OuterHatchAngle { get; set; }
        /// <summary>
        /// Радиус
        /// </summary>
        public double? Radius { get; set; }
        /// <summary>
        /// Имя блока
        /// </summary>
        public string? BlockName { get; set; }
        /// <summary>
        /// Смещение точки вставки блока по оси X
        /// </summary>
        public double? BlockXOffset { get; set; }
        /// <summary>
        /// Смещение точки вставки блока по оси Y
        /// </summary>
        public double? BlockYOffset { get; set; }
        /// <summary>
        /// Путь к файлу для импорта блока
        /// </summary>
        public string? BlockPath { get; set; }

        internal void SaveChanges()
        {
            // UNDONE: Vailidate

            _layerDrawTemplateData.DrawTemplate = DrawTemplate.ToString();
            _layerDrawTemplateData.MarkChar = MarkChar;
            _layerDrawTemplateData.Width = Width;
            _layerDrawTemplateData.Height = Height;
            _layerDrawTemplateData.InnerBorderBrightness = InnerBorderBrightness;
            _layerDrawTemplateData.InnerHatchPattern = InnerHatchPattern;
            _layerDrawTemplateData.InnerHatchScale = InnerHatchScale;
            _layerDrawTemplateData.InnerHatchBrightness = InnerHatchBrightness;
            _layerDrawTemplateData.InnerHatchAngle = InnerHatchAngle;
            _layerDrawTemplateData.FenceWidth = FenceWidth;
            _layerDrawTemplateData.FenceHeight = FenceHeight;
            _layerDrawTemplateData.FenceLayer = FenceLayer;
            _layerDrawTemplateData.OuterHatchPattern = OuterHatchPattern;
            _layerDrawTemplateData.OuterHatchScale = OuterHatchScale;
            _layerDrawTemplateData.OuterHatchBrightness = OuterHatchBrightness;
            _layerDrawTemplateData.OuterHatchAngle = OuterHatchAngle;
            _layerDrawTemplateData.Radius = Radius;
            _layerDrawTemplateData.BlockName = BlockName;
            _layerDrawTemplateData.BlockXOffset = BlockXOffset;
            _layerDrawTemplateData.BlockYOffset = BlockYOffset;
            _layerDrawTemplateData.BlockPath = BlockPath;
        }
    }
}