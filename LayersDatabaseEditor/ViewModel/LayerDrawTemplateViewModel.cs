using LayersDatabaseEditor.Utilities;
using LayersIO.Connection;
using LayersIO.DataTransfer;
using LayersIO.Model;
using NPOI.OpenXmlFormats.Wordprocessing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LayersDatabaseEditor.ViewModel
{
    public class LayerDrawTemplateViewModel : INotifyPropertyChanged
    {
        readonly LayerDrawTemplateData _layerDrawTemplateData;
        private LayersDatabaseContextSqlite _db;
        private DrawTemplate? _drawTemplate;
        private string? _markChar;
        private string? _width;
        private string? _height;
        private double? _innerBorderBrightness;
        private HatchPattern? _innerHatchPattern;
        private double? _innerHatchScale;
        private double? _innerHatchBrightness;
        private double? _innerHatchAngle;
        private string? _fenceWidth;
        private string? _fenceHeight;
        private string? _fenceLayer;
        private HatchPattern? _outerHatchPattern;
        private double? _outerHatchScale;
        private double? _outerHatchBrightness;
        private double? _outerHatchAngle;
        private double? _radius;
        private string? _blockName;
        private double? _blockXOffset;
        private double? _blockYOffset;
        private string? _blockPath;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LayerDrawTemplateViewModel(LayerDrawTemplateData layerDrawTemplateData, LayersDatabaseContextSqlite context)
        {
            _db = context;
            _layerDrawTemplateData = layerDrawTemplateData;
            ResetValues();
        }

        internal LayersDatabaseContextSqlite Database => _db;

        public bool IsUpdated()
        {
            bool updated = _layerDrawTemplateData.DrawTemplate != DrawTemplate.ToString()
                            || _layerDrawTemplateData.MarkChar != MarkChar
                            || _layerDrawTemplateData.Width != Width
                            || _layerDrawTemplateData.Height != Height
                            || _layerDrawTemplateData.InnerBorderBrightness != InnerBorderBrightness
                            || !(string.IsNullOrEmpty(_layerDrawTemplateData.InnerHatchPattern) && InnerHatchPattern == HatchPattern.None) && _layerDrawTemplateData.InnerHatchPattern != InnerHatchPattern.ToString()
                            || _layerDrawTemplateData.InnerHatchScale != InnerHatchScale
                            || _layerDrawTemplateData.InnerHatchBrightness != InnerHatchBrightness
                            || _layerDrawTemplateData.InnerHatchAngle != InnerHatchAngle
                            || _layerDrawTemplateData.FenceWidth != FenceWidth
                            || _layerDrawTemplateData.FenceHeight != FenceHeight
                            || _layerDrawTemplateData.FenceLayer != FenceLayer
                            || !(string.IsNullOrEmpty(_layerDrawTemplateData.OuterHatchPattern) && OuterHatchPattern == HatchPattern.None) && _layerDrawTemplateData.OuterHatchPattern != OuterHatchPattern.ToString()
                            || _layerDrawTemplateData.OuterHatchScale != OuterHatchScale
                            || _layerDrawTemplateData.OuterHatchBrightness != OuterHatchBrightness
                            || _layerDrawTemplateData.OuterHatchAngle != OuterHatchAngle
                            || _layerDrawTemplateData.Radius != Radius
                            || _layerDrawTemplateData.BlockName != BlockName
                            || _layerDrawTemplateData.BlockXOffset != BlockXOffset
                            || _layerDrawTemplateData.BlockYOffset != BlockYOffset
                            || _layerDrawTemplateData.BlockPath != BlockPath;
            return updated;
        }
        /// <summary>
        /// Имя шаблона
        /// </summary>
        public DrawTemplate? DrawTemplate
        {
            get => _drawTemplate;
            set
            {
                _drawTemplate = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Символ для вставки посередине линии
        /// </summary>
        public string? MarkChar
        {
            get => _markChar;
            set
            {
                _markChar = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Ширина (для прямоугольных шаблонов)
        /// </summary>
        public string? Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Высота (для прямоугольных шаблонов)
        /// </summary>
        public string? Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Дополнительная яркость внутреннего прямоугольника  (от - 1 до 1)
        /// </summary>
        public double? InnerBorderBrightness
        {
            get => _innerBorderBrightness;
            set
            {
                _innerBorderBrightness = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Имя образца внутренней штриховки
        /// </summary>
        public HatchPattern? InnerHatchPattern
        {
            get => _innerHatchPattern;
            set
            {
                _innerHatchPattern = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Масштаб внутренней штриховки
        /// </summary>
        public double? InnerHatchScale
        {
            get => _innerHatchScale;
            set
            {
                _innerHatchScale = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Дополнительная яркость внутренней штриховки  (от - 1 до 1)
        /// </summary>
        public double? InnerHatchBrightness
        {
            get => _innerHatchBrightness;
            set
            {
                _innerHatchBrightness = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Угол поворота внутренней штриховки
        /// </summary>
        public double? InnerHatchAngle
        {
            get => _innerHatchAngle;
            set
            {
                _innerHatchAngle = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Ширина внешнего прямоугольника
        /// </summary>
        public string? FenceWidth
        {
            get => _fenceWidth;
            set
            {
                _fenceWidth = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Высота внешнего прямоугольника
        /// </summary>
        public string? FenceHeight
        {
            get => _fenceHeight;
            set
            {
                _fenceHeight = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Слой внешнего прямоугольника (если отличается от основного слоя)
        /// </summary>
        public string? FenceLayer
        {
            get => _fenceLayer;
            set
            {
                _fenceLayer = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Имя образца внешней штриховки
        /// </summary>
        public HatchPattern? OuterHatchPattern
        {
            get => _outerHatchPattern;
            set
            {
                _outerHatchPattern = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Масштаб внешней штриховки
        /// </summary>
        public double? OuterHatchScale
        {
            get => _outerHatchScale;
            set
            {
                _outerHatchScale = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Дополнительная яркость внешней штриховки (от - 1 до 1)
        /// </summary>
        public double? OuterHatchBrightness
        {
            get => _outerHatchBrightness;
            set
            {
                _outerHatchBrightness = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Угол поворота внешней штриховки
        /// </summary>
        public double? OuterHatchAngle
        {
            get => _outerHatchAngle;
            set
            {
                _outerHatchAngle = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Радиус
        /// </summary>
        public double? Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Имя блока
        /// </summary>
        public string? BlockName
        {
            get => _blockName;
            set
            {
                _blockName = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Смещение точки вставки блока по оси X
        /// </summary>
        public double? BlockXOffset
        {
            get => _blockXOffset;
            set
            {
                _blockXOffset = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Смещение точки вставки блока по оси Y
        /// </summary>
        public double? BlockYOffset
        {
            get => _blockYOffset;
            set
            {
                _blockYOffset = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Путь к файлу для импорта блока
        /// </summary>
        public string? BlockPath
        {
            get => _blockPath;
            set
            {
                _blockPath = value;
                OnPropertyChanged();
            }
        }

        internal void UpdateDbEntity()
        {
            // Validation in parent LayerDataViewModel

            _layerDrawTemplateData.DrawTemplate = DrawTemplate.ToString();
            _layerDrawTemplateData.MarkChar = MarkChar;
            _layerDrawTemplateData.Width = Width;
            _layerDrawTemplateData.Height = Height;
            _layerDrawTemplateData.InnerBorderBrightness = InnerBorderBrightness;
            _layerDrawTemplateData.InnerHatchPattern = InnerHatchPattern.ToString();
            _layerDrawTemplateData.InnerHatchScale = InnerHatchScale;
            _layerDrawTemplateData.InnerHatchBrightness = InnerHatchBrightness;
            _layerDrawTemplateData.InnerHatchAngle = InnerHatchAngle;
            _layerDrawTemplateData.FenceWidth = FenceWidth;
            _layerDrawTemplateData.FenceHeight = FenceHeight;
            _layerDrawTemplateData.FenceLayer = FenceLayer;
            _layerDrawTemplateData.OuterHatchPattern = OuterHatchPattern.ToString();
            _layerDrawTemplateData.OuterHatchScale = OuterHatchScale;
            _layerDrawTemplateData.OuterHatchBrightness = OuterHatchBrightness;
            _layerDrawTemplateData.OuterHatchAngle = OuterHatchAngle;
            _layerDrawTemplateData.Radius = Radius;
            _layerDrawTemplateData.BlockName = BlockName;
            _layerDrawTemplateData.BlockXOffset = BlockXOffset;
            _layerDrawTemplateData.BlockYOffset = BlockYOffset;
            _layerDrawTemplateData.BlockPath = BlockPath;
        }

        public void ResetValues()
        {
            DrawTemplate = Enum.Parse<DrawTemplate>(_layerDrawTemplateData.DrawTemplate ?? "Undefined");
            MarkChar = _layerDrawTemplateData.MarkChar;
            Width = _layerDrawTemplateData.Width;
            Height = _layerDrawTemplateData.Height;
            InnerBorderBrightness = _layerDrawTemplateData.InnerBorderBrightness;
            InnerHatchPattern = Enum.Parse<HatchPattern>(_layerDrawTemplateData.InnerHatchPattern ?? "None");
            InnerHatchScale = _layerDrawTemplateData.InnerHatchScale;
            InnerHatchBrightness = _layerDrawTemplateData.InnerHatchBrightness;
            InnerHatchAngle = _layerDrawTemplateData.InnerHatchAngle;
            FenceWidth = _layerDrawTemplateData.FenceWidth;
            FenceHeight = _layerDrawTemplateData.FenceHeight;
            FenceLayer = _layerDrawTemplateData.FenceLayer;
            OuterHatchPattern = Enum.Parse<HatchPattern>(_layerDrawTemplateData.OuterHatchPattern ?? "None");
            OuterHatchScale = _layerDrawTemplateData.OuterHatchScale;
            OuterHatchBrightness = _layerDrawTemplateData.OuterHatchBrightness;
            OuterHatchAngle = _layerDrawTemplateData.OuterHatchAngle;
            Radius = _layerDrawTemplateData.Radius;
            BlockName = _layerDrawTemplateData.BlockName;
            BlockXOffset = _layerDrawTemplateData.BlockXOffset;
            BlockYOffset = _layerDrawTemplateData.BlockYOffset;
            BlockPath = _layerDrawTemplateData.BlockPath;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}