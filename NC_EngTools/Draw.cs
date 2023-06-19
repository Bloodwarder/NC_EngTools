//System
using System.Linq;
using System.Collections.Generic;
using System.IO;

//Modules
using LayerProcessing;
using ExternalData;
using Dictionaries;
//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Teigha.Geometry;
using Platform = HostMgd;
using PlatformDb = Teigha;


using NC_EngTools;
using System;
using Teigha.Colors;
using Legend;

namespace ModelspaceDraw
{
    public abstract class ObjectDraw
    {
        internal List<Entity> EntitiesList { get; } = new List<Entity>();
        internal Point2d Basepoint { get; set; }
        internal RecordLayerParser Layer { get; set; }
        protected static Color s_byLayer = Color.FromColorIndex(ColorMethod.ByLayer, 256);

        internal ObjectDraw() { }
        internal ObjectDraw(Point2d basepoint, RecordLayerParser layer = null)
        {
            Basepoint = basepoint;
            Layer = layer;
        }
        internal abstract void Draw();
        private protected Point2d GetRelativePoint(double x, double y)
        {
            return new Point2d(x + Basepoint.X, y + Basepoint.Y);
        }
    }
    public abstract class LegendObjectDraw : ObjectDraw
    {
        private LegendDrawTemplate legendDrawTemplate;

        internal LegendDrawTemplate LegendDrawTemplate
        {
            get => legendDrawTemplate;
            set
            {
                legendDrawTemplate = value;
                if (TemplateSetEventHandler != null)
                    TemplateSetEventHandler(this, new EventArgs());
            }
        }
        protected event EventHandler TemplateSetEventHandler;

        internal LegendObjectDraw() { }
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

    public class SolidLineDraw : LegendObjectDraw
    {
        public SolidLineDraw() { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal SolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        internal override void Draw()
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

    public class DoubleSolidLineDraw : LegendObjectDraw
    {
        public DoubleSolidLineDraw() { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal DoubleSolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        internal override void Draw()
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
        internal override void Draw()
        {
            DrawText();
            List<Polyline> polylines = DrawLines();
            FormatLines(polylines);
        }

        private protected void DrawText()
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
        private protected List<Polyline> DrawLines()
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
        protected abstract void FormatLines(IEnumerable<Polyline> lines);

    }
    public class MarkedSolidLineDraw : MarkedLineDraw
    {
        public MarkedSolidLineDraw() { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        protected override void FormatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(Layer.TrueName, out _, true).ConstantWidth;
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
    public class MarkedDashedLineDraw : MarkedLineDraw
    {
        public MarkedDashedLineDraw() { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal MarkedDashedLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        protected override void FormatLines(IEnumerable<Polyline> lines)
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

    public abstract class AreaDraw : LegendObjectDraw
    {
        public AreaDraw() { }

        internal AreaDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal AreaDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        private protected void DrawHatch(IEnumerable<Polyline> borders, string patternname = "SOLID", double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8)
        {
            Hatch hatch = new Hatch();

            hatch.SetHatchPattern(!patternname.Contains("ANSI") ? HatchPatternType.PreDefined : HatchPatternType.UserDefined, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
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

    public class RectangleDraw : AreaDraw
    {
        public RectangleDraw() { }

        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal RectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate.Width, LegendGrid.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate.Height, LegendGrid.CellHeight);


        internal override void Draw()
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
            //Workstation.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(rectangle, true);
            return rectangle;
        }
    }

    public class HatchedRectangleDraw : RectangleDraw
    {
        public HatchedRectangleDraw() { }

        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal override void Draw()
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

    public class HatchedCircleDraw : AreaDraw
    {
        internal double Radius => LegendDrawTemplate.Radius;
        public HatchedCircleDraw() { }
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedCircleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        internal override void Draw()
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
            circle.AddVertexAt(0, GetRelativePoint(0, radius / 2), radius, 0d, 0d);
            circle.AddVertexAt(1, GetRelativePoint(0, -radius / 2), radius, 0d, 0d);
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

    public class FencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceHeight, LegendGrid.CellHeight);
        public FencedRectangleDraw() { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal override void Draw()
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

    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => ParseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellHeight);
        public HatchedFencedRectangleDraw() { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal override void Draw()
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


        internal override void Draw()
        {
            // Перед отрисовкой первого объекта импортируем все блоки в очереди
            if (!_blocksImported)
            {
                ImportRecords(out HashSet<string> _);
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
            // По одному разу открываем каждый файл с блоками для условных
            foreach (string file in QueuedFiles)
            {
                Document doc = Application.DocumentManager.Open(file, true);
                doc.Window.Visible = false;
                try
                {
                    // Ищем все нужные нам блоки
                    BlockTable blockTable = transaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    var blocks = (from ObjectId blockId in blockTable
                                  let block = transaction.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord
                                  where QueuedBlocks[file].Contains(block.Name)
                                  select block).ToList();
                    // Заполняем сет с ненайденными блоками
                    foreach (BlockTableRecord block in blocks)
                    {
                        if (!QueuedBlocks[file].Contains(block.Name))
                            failedBlockImports.Add(block.Name);
                    }
                    // Добавляем все найденные блоки в таблицу блоков текущего чертежа
                    blockTable.Database.WblockCloneObjects(new ObjectIdCollection(blocks.Select(b => b.ObjectId).ToArray()), _blocktable.ObjectId, new IdMapping(), DuplicateRecordCloning.Ignore, false);

                }
                finally
                {
                    // Закрываем файл и убираем из очереди
                    doc.CloseAndDiscard();
                    QueuedBlocks.Remove(file);
                }
            }
            QueuedFiles.Clear();
        }
    }

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

        internal override void Draw()
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

    public struct LegendDrawTemplate
    {
        public string DrawTemplate;
        public string MarkChar;
        public string Width;
        public string Height;
        public double InnerBorderBrightness;
        public string InnerHatchPattern;
        public double InnerHatchScale;
        public double InnerHatchBrightness;
        public double InnerHatchAngle;
        public string FenceWidth;
        public string FenceHeight;
        public string FenceLayer;
        public string OuterHatchPattern;
        public double OuterHatchScale;
        public double OuterHatchBrightness;
        public double OuterHatchAngle;
        public double Radius;
        public string BlockName;
        public double BlockXOffset;
        public double BlockYOffset;
        public string BlockPath;
    }



    internal class SimpleTestObjectDraw : LegendObjectDraw
    {
        public SimpleTestObjectDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        internal override void Draw()
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
