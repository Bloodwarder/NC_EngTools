//System
using System;
using System.Linq;
using System.Collections.Generic;

//Modules
using NC_EngTools;
using Legend;
using LayerProcessing;
using ExternalData;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Colors;




namespace ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки объекта чертежа
    /// </summary>
    public abstract class ObjectDraw
    {
        /// <summary>
        /// Список созданных объектов чертежа для вставки в модель целевого чертежа. Заполняется через метод Draw
        /// </summary>
        public List<Entity> EntitiesList { get; } = new List<Entity>();
        /// <summary>
        /// Базовая точка для вставки объектов в целевой чертёж
        /// </summary>
        public Point2d Basepoint { get; set; }
        internal RecordLayerParser Layer { get; set; }
        internal static Color s_byLayer = Color.FromColorIndex(ColorMethod.ByLayer, 256);

        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal protected ObjectDraw() { }
        internal ObjectDraw(Point2d basepoint, RecordLayerParser layer = null)
        {
            Basepoint = basepoint;
            Layer = layer;
        }
        /// <summary>
        /// Создать объекты чертежа для отрисовки (последующей вставки в модель целевого чертежа). После выполнения доступны через свойство EntitiesList.
        /// </summary>
        public abstract void Draw();
        /// <summary>
        /// Преобразование относительных координат в сетке условных в абсолютные координаты целевого чертежа
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected Point2d GetRelativePoint(double x, double y)
        {
            return new Point2d(x + Basepoint.X, y + Basepoint.Y);
        }
    }
    /// <summary>
    /// Класс для отрисовки элемента легенды
    /// </summary>
    public abstract class LegendObjectDraw : ObjectDraw
    {
        private LegendDrawTemplate legendDrawTemplate;
        /// <summary>
        /// Структура с данными для отрисовки объекта
        /// </summary>
        public LegendDrawTemplate LegendDrawTemplate
        {
            get => legendDrawTemplate;
            set
            {
                legendDrawTemplate = value;
                TemplateSetEventHandler?.Invoke(this, new EventArgs());
            }
        }
        /// <summary>
        /// Событие назначения объекту конкретного шаблона отрисовки.
        /// Необходимо для объектов, которые нельзя обрабатывать одномоментно и нужно поставить в очередь на обработку (например блоки, импортируемые из внешних чертежей).
        /// </summary>
        protected event EventHandler TemplateSetEventHandler;

        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal protected LegendObjectDraw() { }
        internal LegendObjectDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
            LegendDrawTemplate = LayerLegendDrawDictionary.GetValue(Layer.TrueName, out _);
        }
        internal LegendObjectDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal static double CellWidth => LegendGrid.CellWidth;
        internal static double CellHeight => LegendGrid.CellHeight;

        private protected static double ParseRelativeValue(string value, double absolute)
        {
            return value.EndsWith("*") ? double.Parse(value.Replace("*", "")) * absolute : double.Parse(value);
        }

        private protected Color BrightnessShift(double value)
        {
            if (value == 0)
                return s_byLayer;
            Color color = Layer.BoundLayer.Color;
            if (value > 0)
            {
                color = Color.FromRgb((byte)(color.Red + (255 - color.Red) * value), (byte)(color.Green + (255 - color.Green) * value), (byte)(color.Blue + (255 - color.Blue) * value));
            }
            else if (value < 0)
            {
                color = Color.FromRgb((byte)(color.Red + color.Red * value), (byte)(color.Green + color.Green * value), (byte)(color.Blue + color.Blue * value));
            }
            return color;
        }
    }

    /// <summary>
    /// Класс для отрисовки непрерывной линии
    /// </summary>
    public class SolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public SolidLineDraw() { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
/// <inheritdoc/>
        public override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.OutputLayerName;
            LayerProps lp = LayerPropertiesDictionary.GetValue(Layer.TrueName, out bool success);
            if (success)
            {
                pl.LinetypeScale = lp.LTScale;
                pl.ConstantWidth = lp.ConstantWidth;
            }
            EntitiesList.Add(pl);
        }
    }

    /// <summary>
    /// Класс для отрисовки двойной линии (одна над другой, верхняя линия по стандарту слоя)
    /// </summary>
    public class DoubleSolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public DoubleSolidLineDraw() { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
/// <inheritdoc/>
        public override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.OutputLayerName;
            Polyline pl2 = pl.Clone() as Polyline;
            LayerProps lp = LayerPropertiesDictionary.GetValue(Layer.TrueName, out bool success);
            if (success)
            {
                pl.LinetypeScale = lp.LTScale;
                pl.ConstantWidth = lp.ConstantWidth;
            }
            pl2.ConstantWidth = double.Parse(LegendDrawTemplate.Width);  // ТОЖЕ КОСТЫЛЬ, ЧТОБЫ НЕ ДОБАВЛЯТЬ ДОП ПОЛЕ В ТАБЛИЦУ. ТАКИХ СЛОЯ ВСЕГО 3
            pl2.Color = Color.FromRgb(0, 0, 255);   // ЗАТЫЧКА, ПОКА ТАКОЙ ОБЪЕКТ ВСЕГО ОДИН
            EntitiesList.Add(pl2);
            EntitiesList.Add(pl);
        }
    }

    /// <summary>
    /// Класс для отрисовки линии с вставленными символами (буквами)
    /// </summary>
    public abstract class MarkedLineDraw : LegendObjectDraw
    {
        internal string MarkChar => LegendDrawTemplate.MarkChar;
        private double _width;

        internal MarkedLineDraw() { }
        internal MarkedLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
        }
        internal MarkedLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            DrawText();
            List<Polyline> polylines = DrawLines();
            FormatLines(polylines);
        }

        /// <summary>
        /// Создание текста для линии и его добавление в список объектов для отрисовки
        /// </summary>
        protected void DrawText()
        {
            TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            MText mtext = new MText
            {
                Contents = MarkChar,
                TextStyleId = txtstyletable["Standard"],
                TextHeight = 4d,
                Layer = Layer.BoundLayer.Name,
                Color = s_byLayer
            };
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            _width = mtext.ActualWidth;
            EntitiesList.Add(mtext);
        }
        /// <summary>
        /// Создание полилиний и добавление их в список объектов для отрисовки
        /// </summary>
        /// <returns> Полилинии, добавленные в список объектов для отрисовки</returns>
        protected List<Polyline> DrawLines()
        {
            Polyline pl1 = new Polyline();
            Polyline pl2 = new Polyline();
            pl1.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl1.AddVertexAt(1, GetRelativePoint(-(_width / 2 + 0.5d), 0d), 0, 0d, 0d);
            pl1.Layer = Layer.BoundLayer.Name;

            pl2.AddVertexAt(0, GetRelativePoint(_width / 2 + 0.5d, 0d), 0, 0d, 0d);
            pl2.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl2.Layer = Layer.BoundLayer.Name;

            List<Polyline> list = new List<Polyline> { pl1, pl2 };
            EntitiesList.AddRange(list);
            return list;
        }
        /// <summary>
        /// Придание нужных свойств линиям, созданным для данного объекта отрисовки
        /// </summary>
        /// <param name="lines">Коллекция полилиний для обработки</param>
        protected abstract void FormatLines(IEnumerable<Polyline> lines);

    }
    /// <summary>
    /// Класс для отрисовки сплошной линии с символьным (буквенным) обозначением
    /// </summary>
    public class MarkedSolidLineDraw : MarkedLineDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public MarkedSolidLineDraw() { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
         /// <inheritdoc/>
         protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(Layer.TrueName, out _, true).ConstantWidth;
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
    /// <summary>
    /// Класс для отрисовки пунктирной линии с символьным (буквенным) обозначением
    /// </summary>
    public class MarkedDashedLineDraw : MarkedLineDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public MarkedDashedLineDraw() { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected sealed override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(Layer.TrueName, out _, true).ConstantWidth;
                LayerChecker.CheckLinetype("ACAD_ISO02W100", out bool ltgetsuccess);
                if (ltgetsuccess)
                    line.Linetype = "ACAD_ISO02W100";
                line.LinetypeScale = 0.3d;
            }
        }
    }

    /// <summary>
    /// Абстрактный класс для отрисовки заштрихованных объектов
    /// </summary>
    public abstract class AreaDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public AreaDraw() { }
                internal AreaDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal AreaDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        /// <summary>
        /// Отрисовка штриховки
        /// </summary>
        /// <param name="borders"> Объекты контура </param>
        /// <param name="patternname"> Имя образца </param>
        /// <param name="patternscale"> Масштаб образца </param>
        /// <param name="angle"> Угол поворота </param>
        /// <param name="increasebrightness"> Изменение яркости относительно базового цвета слоя </param>
        protected void DrawHatch(IEnumerable<Polyline> borders, string patternname = "SOLID", double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8)
        {
            Hatch hatch = new Hatch();
            //ДИКИЙ БЛОК, ПЫТАЮЩИЙСЯ ОБРАБОТАТЬ ОШИБКИ ДЛЯ НЕПОНЯТНЫХ ШТРИХОВОК
            try
            {
                hatch.SetHatchPattern(!patternname.Contains("ANSI") ? HatchPatternType.PreDefined : HatchPatternType.UserDefined, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
            }
            catch
            {

                for (int i = 2; i > -1; i--)
                {
                    try
                    {
                        hatch.SetHatchPattern((HatchPatternType)i, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            hatch.HatchStyle = HatchStyle.Normal;
            foreach (Polyline pl in borders)
            {
                Point2dCollection vertexCollection = new Point2dCollection(pl.NumberOfVertices);
                DoubleCollection bulgesCollection = new DoubleCollection(pl.NumberOfVertices);
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    vertexCollection.Add(pl.GetPoint2dAt(i));
                    bulgesCollection.Add(pl.GetBulgeAt(i));
                }
                hatch.AppendLoop(HatchLoopTypes.Polyline, vertexCollection, bulgesCollection);
            }
            if (patternname != "SOLID")
            {
                hatch.PatternAngle = angle * Math.PI / 180;
                hatch.PatternScale = patternscale;
                if (increasebrightness != 0)
                    hatch.BackgroundColor = BrightnessShift(increasebrightness);
            }
            else
            {
                hatch.Color = BrightnessShift(increasebrightness);
            }
            hatch.Layer = Layer.BoundLayer.Name;
            ;
            EntitiesList.Add(hatch);
        }
    }

    /// <summary>
    /// Класс для отрисовки пустого прямоугольника
    /// </summary>
    public class RectangleDraw : AreaDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public RectangleDraw() { }

        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate.Width, LegendGrid.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate.Height, LegendGrid.CellHeight);
/// <inheritdoc/>
        public override void Draw()
        {
            DrawRectangle(RectangleWidth, RectangleHeight);
        }

        private protected Polyline DrawRectangle(double width, double height, string layer = null, double brightnessshift = 0d)
        {
            Polyline rectangle = new Polyline();
            rectangle.AddVertexAt(0, GetRelativePoint(-width / 2, -height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(1, GetRelativePoint(-width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, GetRelativePoint(width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(3, GetRelativePoint(width / 2, -height / 2), 0, 0d, 0d);
            rectangle.Closed = true;
            if (layer != null)
                LayerChecker.Check($"{LayerParser.StandartPrefix}_{layer}"); //ПОКА ЗАВЯЗАНО НА ЧЕКЕР ИЗ ДРУГОГО МОДУЛЯ. ПРОАНАЛИЗИРОВАТЬ ВОЗМОЖНОСТИ ОПТИМИЗАЦИИ
            rectangle.Layer = layer == null ? Layer.BoundLayer.Name : $"{LayerParser.StandartPrefix}_{layer}";
            LayerProps lp = LayerPropertiesDictionary.GetValue(rectangle.Layer, out bool success);
            if (success)
            {
                rectangle.LinetypeScale = lp.LTScale;
                rectangle.ConstantWidth = lp.ConstantWidth;
            }
            rectangle.Color = BrightnessShift(brightnessshift);
            EntitiesList.Add(rectangle);
            return rectangle;
        }
    }

    /// <summary>
    /// Класс для отрисовки заштрихованного прямоугольника
    /// </summary>
    public class HatchedRectangleDraw : RectangleDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedRectangleDraw() { }

        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

/// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { DrawRectangle(RectangleWidth, RectangleHeight, brightnessshift: LegendDrawTemplate.InnerBorderBrightness) };
            DrawHatch(rectangle,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
        }
    }

    /// <summary>
    /// Класс для отрисовки заштрихованного круга
    /// </summary>
    public class HatchedCircleDraw : AreaDraw
    {
        internal double Radius => LegendDrawTemplate.Radius;
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedCircleDraw() { }
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> circle = new List<Polyline> { DrawCircle(Radius) };
            DrawHatch(circle,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
        }

        private protected Polyline DrawCircle(double radius, string layer = null)
        {
            Polyline circle = new Polyline();
            circle.AddVertexAt(0, GetRelativePoint(0, radius / 2), 1, 0d, 0d);
            circle.AddVertexAt(1, GetRelativePoint(0, -radius / 2), 1, 0d, 0d);
            circle.AddVertexAt(2, GetRelativePoint(0, radius / 2), 0, 0d, 0d);
            circle.Closed = true;
            circle.Layer = layer ?? Layer.BoundLayer.Name;
            LayerProps lp = LayerPropertiesDictionary.GetValue(circle.Layer, out bool success);
            if (success)
            {
                circle.LinetypeScale = lp.LTScale;
                circle.ConstantWidth = lp.ConstantWidth;
            }

            EntitiesList.Add(circle);
            return circle;
        }
    }

    /// <summary>
    /// Класс для отрисовки заштрихованного прямоугольника внутри другого прямоугольника-ограждения
    /// </summary>
    public class FencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceHeight, LegendGrid.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public FencedRectangleDraw() { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

/// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { DrawRectangle(RectangleWidth, RectangleHeight, brightnessshift: LegendDrawTemplate.InnerBorderBrightness) };
            DrawHatch(rectangle,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
            DrawRectangle(FenceWidth, FenceHeight, FenceLayer);
        }
    }

    /// <summary>
    /// Класс для отрисовки двух прямоугольников с двумя штриховками
    /// </summary>
    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellHeight);
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public HatchedFencedRectangleDraw() { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
/// <inheritdoc/>
        public override void Draw()
        {
            List<Polyline> rectangles = new List<Polyline>
                {
                DrawRectangle
                    (
                    RectangleWidth,
                    RectangleHeight,
                    brightnessshift: LegendDrawTemplate.InnerBorderBrightness
                    )
                };
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate.InnerHatchPattern,
                angle: LegendDrawTemplate.InnerHatchAngle,
                patternscale: LegendDrawTemplate.InnerHatchScale,
                increasebrightness: LegendDrawTemplate.InnerHatchBrightness
                );
            rectangles.Add(DrawRectangle(FenceWidth, FenceHeight, FenceLayer));
            DrawHatch
                (
                rectangles,
                patternname: LegendDrawTemplate.OuterHatchPattern,
                angle: LegendDrawTemplate.OuterHatchAngle,
                patternscale: LegendDrawTemplate.OuterHatchScale,
                increasebrightness: LegendDrawTemplate.OuterHatchBrightness
                );
        }
    }

    /// <summary>
    /// Отрисовывает вхождение блока
    /// </summary>
    public class BlockReferenceDraw : LegendObjectDraw
    {
        private string BlockName => LegendDrawTemplate.BlockName;
        private string FilePath => LegendDrawTemplate.BlockPath;

        // Списки файлов и блоков для единоразовой обработки
        internal static HashSet<string> QueuedFiles = new HashSet<string>();
        internal static Dictionary<string, HashSet<string>> QueuedBlocks = new Dictionary<string, HashSet<string>>();
        // Проверка, выполнен ли иморт блоков
        private static bool _blocksImported = false;

        private static BlockTable _blocktable;

        static BlockReferenceDraw()
        {
            _blocktable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;
        }
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public BlockReferenceDraw()
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

/// <inheritdoc/>
        public override void Draw()
        {
            // Перед отрисовкой первого объекта импортируем все блоки в очереди
            if (!_blocksImported)
            {
                ImportRecords(out HashSet<string> failedImports);
                foreach (string str in failedImports)
                    Workstation.Editor.WriteMessage($"Не удалось импортировать блок {str}");
                _blocksImported = true;
            }
            // Отрисовываем объект
            ObjectId btrId = _blocktable[BlockName];
            BlockReference bref = new BlockReference(new Point3d(Basepoint.X + LegendDrawTemplate.BlockXOffset, Basepoint.Y + LegendDrawTemplate.BlockYOffset, 0d), btrId);
            bref.Layer = Layer.BoundLayer.Name;
            EntitiesList.Add(bref);
        }

        private void QueueImportBlockTableRecord(object sender, System.EventArgs e)
        {
            if (_blocktable.Has(BlockName))
                return;

            if (QueuedFiles.Count == 0)
            {
                // Обновляем таблицу блоков (по открытому чертежу)
                _blocktable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;
                // Сбрасывем переменную, показывающую, что импорт для данной задачи выполнен
                _blocksImported = false;
            }

            // Заполняем очереди блоков и файлов для импорта
            QueuedFiles.Add(LegendDrawTemplate.BlockPath);
            HashSet<string> blocksQueue;
            bool success = QueuedBlocks.TryGetValue(FilePath, out blocksQueue);
            if (!success)
                QueuedBlocks.Add(FilePath, blocksQueue = new HashSet<string>());
            blocksQueue.Add(BlockName);
        }

        private static void ImportRecords(out HashSet<string> failedBlockImports)
        {
            failedBlockImports = new HashSet<string>();
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            // По одному разу открываем базу данных каждого файла с блоками для условных
            foreach (string file in QueuedFiles)
            {
                using (Database importDatabase = new Database(false, true))
                {
                    try
                    {
                        importDatabase.ReadDwgFile(file, FileOpenMode.OpenForReadAndAllShare, false, null);
                        // Ищем все нужные нам блоки
                        BlockTable importBlockTable = transaction.GetObject(importDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                        var importedBlocks = (from ObjectId blockId in importBlockTable
                                              let block = transaction.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord
                                              where QueuedBlocks[file].Contains(block.Name)
                                              select block).ToList();
                        // Заполняем сет с ненайденными блоками
                        foreach (BlockTableRecord block in importedBlocks)
                        {
                            if (!QueuedBlocks[file].Contains(block.Name))
                                failedBlockImports.Add(block.Name);
                        }
                        // Добавляем все найденные блоки в таблицу блоков текущего чертежа
                        importBlockTable.Database.WblockCloneObjects(new ObjectIdCollection(importedBlocks.Select(b => b.ObjectId).ToArray()), _blocktable.ObjectId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                    }
                    catch (System.Exception)
                    {
                        Workstation.Editor.WriteMessage($"Ошибка импорта блоков из файла \"{file}\"");
                    }
                    finally
                    {
                        // Убираем файл из очереди
                        QueuedBlocks.Remove(file);
                    }
                }
            }
            QueuedFiles.Clear();
        }
    }

    /// <summary>
    /// Отрисовывает надпись
    /// </summary>
    public class LabelTextDraw : ObjectDraw
    {
        private readonly bool _italic = false;
        private readonly string _text;
        static LabelTextDraw()
        {
            LayerChecker.Check(string.Concat(LayerParser.StandartPrefix, "_Условные"));
        }
        internal LabelTextDraw() { }
        internal LabelTextDraw(Point2d basepoint, string label, bool italic = false) : base()
        {
            Basepoint = basepoint;
            _italic = italic;
            _text = label;
        }

/// <inheritdoc/>
        public override void Draw()
        {
            TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            string legendTextLayer = string.Concat(LayerParser.StandartPrefix, "_Условные");
            LayerChecker.Check(legendTextLayer);
            MText mtext = new MText
            {
                Contents = _text,
                TextStyleId = txtstyletable["Standard"],
                TextHeight = LegendGrid.TextHeight,
                Layer = legendTextLayer,
                Color = s_byLayer
            };
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleLeft);

            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            EntitiesList.Add(mtext);
        }
    }

    /// <summary>
    /// Данные для отрисовки шаблона легенды
    /// </summary>
    public struct LegendDrawTemplate
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



    internal class SimpleTestObjectDraw : LegendObjectDraw
    {
        public SimpleTestObjectDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        public override void Draw()
        {


            Database db = Workstation.Database;

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable blocktable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                BlockTableRecord modelspace = transaction.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;

                BlockTableRecord newbtr = new BlockTableRecord
                {
                    Name = "NewBlock",
                    Explodable = true
                };
                blocktable.Add(newbtr);
                transaction.AddNewlyCreatedDBObject(newbtr, true); // и в транзакцию

                //отрезок
                Line line = new Line(new Point3d(new double[3] { 0d, 0d, 0d }), new Point3d(new double[3] { 2d, 2d, 0d }));
                newbtr.AppendEntity(line); // добавляем в модельное пространство
                transaction.AddNewlyCreatedDBObject(line, true); // и в транзакцию

                //полилиния
                Polyline pl = new Polyline();
                pl.AddVertexAt(0, new Point2d(-5d, 0d), 0, 0.2d, 0.2d);
                pl.AddVertexAt(1, new Point2d(-0d, 0d), 1, 0.2d, 0.2d);
                pl.AddVertexAt(2, new Point2d(-0d, 5d), 0, 0.2d, 0.2d);
                pl.SetPointAt(0, new Point2d(-10d, 0d));
                pl.Closed = true;
                ObjectId plid = newbtr.AppendEntity(pl); // добавляем в модельное пространство


                //штриховка
                Hatch hatch = new Hatch();
                hatch.SetHatchPattern(HatchPatternType.UserDefined, "ANSI31");
                hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(new ObjectId[] { plid }));
                hatch.PatternAngle = (double)(45 * Math.PI / 180);
                Color color = Color.FromRgb(255, 0, 0);
                double m = 0.8d;
                color = Color.FromRgb((byte)(color.Red + (255 - color.Red) * m), (byte)(color.Green + (255 - color.Green) * m), (byte)(color.Blue + (255 - color.Blue) * m));
                hatch.BackgroundColor = color;
                newbtr.AppendEntity(hatch); // добавляем в модельное пространство

                //порядок прорисовки
                DrawOrderTable dro = (DrawOrderTable)transaction.GetObject(newbtr.DrawOrderTableId, OpenMode.ForWrite);
                dro.MoveBelow(new ObjectIdCollection(new ObjectId[] { hatch.ObjectId }), plid);

                //вхождение блока
                BlockReference bref = new BlockReference(new Point3d(new double[] { 0d, 5d, 0d }), newbtr.ObjectId);
                modelspace.AppendEntity(bref);
                transaction.AddNewlyCreatedDBObject(bref, true); // и в транзакцию
                                                                 //using (DBObjectCollection dbObjects = new DBObjectCollection())
                                                                 //{
                                                                 //    bref.Explode(dbObjects);
                                                                 //    foreach (DBObject obj in dbObjects)
                                                                 //    {
                                                                 //        Entity ent = obj as Entity;
                                                                 //        modelspace.AppendEntity(ent);
                                                                 //        transaction.AddNewlyCreatedDBObject(ent, true);
                                                                 //    }
                                                                 //}

                //многострочный текст
                TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                MText mtext = new MText
                {
                    BackgroundFill = true,
                    UseBackgroundColor = true,
                    BackgroundScaleFactor = 1.1d,
                    TextStyleId = txtstyletable["Standard"],
                    Contents = "TEST_TEXT",
                    TextHeight = 4d
                };
                mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
                mtext.Location = new Point3d(-2d, -2d, 0d);
                modelspace.AppendEntity(mtext);
                transaction.AddNewlyCreatedDBObject(mtext, true); // и в транзакцию

                transaction.Commit();
            }
        }
    }
}
