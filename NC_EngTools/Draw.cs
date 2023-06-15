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
        private protected Point2d relativePoint(double x, double y)
        {
            return new Point2d(x+Basepoint.X, y+Basepoint.Y);
        }
    }
    public abstract class LegendObjectDraw : ObjectDraw
    {
        internal LegendDrawTemplate LegendDrawTemplate { get; set; }

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

        private protected static double parseRelativeValue(string value, double absolute)
        {
            return value.EndsWith("*") ? double.Parse(value.Replace("*", ""))*absolute : double.Parse(value);
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
            pl.AddVertexAt(0, relativePoint(-CellWidth/2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, relativePoint(CellWidth/2, 0d), 0, 0d, 0d);
            pl.Layer = Layer.OutputLayerName;
            LayerProps lp = LayerPropertiesDictionary.GetValue(Layer.TrueName, out bool success);
            if (success)
            {
                pl.LinetypeScale=lp.LTScale;
                pl.ConstantWidth=lp.ConstantWidth;
            }
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
            drawText();
            List<Polyline> polylines = drawLines();
            formatLines(polylines);
        }

        private protected void drawText()
        {
            MText mtext = new MText();
            mtext.Contents = MarkChar;
            TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            mtext.TextStyleId =txtstyletable["Standard"];
            mtext.TextHeight = 4d;
            mtext.Layer = Layer.BoundLayer.Name;
            mtext.Color = s_byLayer;
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            _width = mtext.ActualWidth;
            EntitiesList.Add(mtext);

        }
        private protected List<Polyline> drawLines()
        {
            Polyline pl1 = new Polyline();
            Polyline pl2 = new Polyline();
            pl1.AddVertexAt(0, relativePoint(-CellWidth/2, 0d), 0, 0d, 0d);
            pl1.AddVertexAt(1, relativePoint(-(_width/2+0.5d), 0d), 0, 0d, 0d);
            pl1.Layer = Layer.BoundLayer.Name;

            pl2.AddVertexAt(0, relativePoint(_width/2+0.5d, 0d), 0, 0d, 0d);
            pl2.AddVertexAt(1, relativePoint(CellWidth/2, 0d), 0, 0d, 0d);
            pl2.Layer = Layer.BoundLayer.Name;

            List<Polyline> list = new List<Polyline> { pl1, pl2 };
            EntitiesList.AddRange(list);
            return list;
        }
        protected abstract void formatLines(IEnumerable<Polyline> lines);

    }
    public class MarkedSolidLineDraw : MarkedLineDraw
    {
        public MarkedSolidLineDraw() { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        protected override void formatLines(IEnumerable<Polyline> lines)
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
        protected override void formatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(Layer.TrueName, out _, true).ConstantWidth;
                LayerChecker.CheckLinetype("ACAD_ISO02W100", out bool ltgetsuccess);
                if (ltgetsuccess)
                    line.Linetype = "ACAD_ISO02W100";
                line.LinetypeScale=0.3d;
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

        private protected void drawHatch(IEnumerable<Polyline> borders, string patternname = "SOLID", double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8, Color background = null)
        {
            Hatch hatch = new Hatch();
            hatch.SetHatchPattern(HatchPatternType.UserDefined, patternname);
            hatch.HatchStyle = HatchStyle.Normal;
            hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(borders.Select(o => o.ObjectId).ToArray()));
            hatch.PatternAngle = angle *Math.PI/180;
            hatch.PatternScale = patternscale;
            Color color = Layer.BoundLayer.Color;
            color = Color.FromRgb((byte)(color.Red+(255-color.Red)*increasebrightness), (byte)(color.Green+(255-color.Green)*increasebrightness), (byte)(color.Blue+(255-color.Blue)*increasebrightness));
            hatch.BackgroundColor = background;
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

        internal double RectangleWidth => parseRelativeValue(LegendDrawTemplate.Width, LegendGrid.CellWidth);
        internal double RectangleHeight => parseRelativeValue(LegendDrawTemplate.Height, LegendGrid.CellHeight);


        internal override void Draw()
        {
            drawRectangle(RectangleWidth, RectangleHeight);
        }

        private protected Polyline drawRectangle(double width, double height, string layer = null)
        {
            Polyline rectangle = new Polyline();
            rectangle.AddVertexAt(0, relativePoint(-width/2, -height/2), 0, 0d, 0d);
            rectangle.AddVertexAt(1, relativePoint(-width/2, height/2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, relativePoint(width/2, height/2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, relativePoint(width/2, -height/2), 0, 0d, 0d);
            rectangle.Closed = true;
            rectangle.Layer = layer == null ? Layer.BoundLayer.Name : layer;
            LayerProps lp = LayerPropertiesDictionary.GetValue(rectangle.Layer, out bool success);
            if (success)
            {
                rectangle.LinetypeScale=lp.LTScale;
                rectangle.ConstantWidth=lp.ConstantWidth;
            }

            EntitiesList.Add(rectangle);
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
            List<Polyline> rectangle = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangle);
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
            List<Polyline> circle = new List<Polyline> { drawCircle(Radius) };
            drawHatch(circle);
        }

        private protected Polyline drawCircle(double radius, string layer = null)
        {
            Polyline circle = new Polyline();
            circle.AddVertexAt(0, relativePoint(0, radius/2), radius, 0d, 0d);
            circle.AddVertexAt(1, relativePoint(0, -radius/2), radius, 0d, 0d);
            circle.AddVertexAt(2, relativePoint(0, radius/2), 0, 0d, 0d);
            circle.Closed = true;
            circle.Layer = layer==null ? Layer.BoundLayer.Name : layer;
            LayerProps lp = LayerPropertiesDictionary.GetValue(circle.Layer, out bool success);
            if (success)
            {
                circle.LinetypeScale=lp.LTScale;
                circle.ConstantWidth=lp.ConstantWidth;
            }

            EntitiesList.Add(circle);
            return circle;
        }
    }

    public class FencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => parseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => parseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellHeight);
        public FencedRectangleDraw() { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangle);
            drawRectangle(FenceWidth, FenceHeight, FenceLayer);
        }
    }

    public class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer => LegendDrawTemplate.FenceLayer;
        internal double FenceWidth => parseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellWidth);
        internal double FenceHeight => parseRelativeValue(LegendDrawTemplate.FenceWidth, LegendGrid.CellHeight);
        public HatchedFencedRectangleDraw() { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal override void Draw()
        {
            List<Polyline> rectangles = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangles);
            rectangles.Add(drawRectangle(FenceWidth, FenceHeight, FenceLayer));
            drawHatch(rectangles);
        }
    }

    public class BlockReferenceDraw : LegendObjectDraw
    {
        internal string BlockName { get; set; }
        internal string FilePath { get; set; }
        BlockTable _blocktable;
        public BlockReferenceDraw()
        {
            _blocktable = Workstation.TransactionManager.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
            _blocktable = Workstation.TransactionManager.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            _blocktable = Workstation.TransactionManager.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
            LegendDrawTemplate = template;
        }


        internal override void Draw()
        {
            BlockReference bref = new BlockReference(new Point3d(Basepoint.X, Basepoint.Y, 0d), findBlockTableRecord(BlockName));
            EntitiesList.Add(bref);
        }

        private ObjectId findBlockTableRecord(string blockname)
        {
            var blocktablerecordid = from ObjectId elem in _blocktable
                                     let btr = Workstation.TransactionManager.GetObject(elem, OpenMode.ForRead) as BlockTableRecord
                                     where btr.Name == blockname
                                     select elem;
            return blocktablerecordid.FirstOrDefault();
        }

        private void importBlockTableRecord(string blockname, string filepath)
        {
            if (_blocktable.Has(blockname))
                return;
            throw new NotImplementedException();
        }
    }

    public class LabelTextDraw : ObjectDraw
    {
        private bool _italic = false;
        private string _text;
        static LabelTextDraw()
        {
            LayerChecker.Check(string.Concat(LayerParser.StandartPrefix, "_Условные"));
        }
        internal LabelTextDraw() { }
        internal LabelTextDraw(Point2d basepoint, string label, bool italic = false) : base()
        {
            Basepoint = basepoint;
            _italic=italic;
            _text = label;
        }

        internal override void Draw()
        {
            MText mtext = new MText();
            mtext.Contents = _text;
            TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            mtext.TextStyleId = txtstyletable["Standard"];
            mtext.TextHeight = LegendGrid.TextHeight;
            mtext.Layer = string.Concat(LayerParser.StandartPrefix, "_Условные");
            mtext.Color = s_byLayer;
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

                BlockTableRecord newbtr = new BlockTableRecord();
                newbtr.Name = "NewBlock";
                newbtr.Explodable = true;
                blocktable.Add(newbtr);
                transaction.AddNewlyCreatedDBObject(newbtr, true); // и в транзакцию

                //отрезок
                Line line = new Line(new Point3d(new double[3] { 0d, 0d, 0d }), new Point3d(new double[3] { 2d, 2d, 0d }));
                ObjectId lid = newbtr.AppendEntity(line); // добавляем в модельное пространство
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
                hatch.PatternAngle = (double)(45 *Math.PI/180);
                Color color = Color.FromRgb(255, 0, 0);
                double m = 0.8d;
                color = Color.FromRgb((byte)(color.Red+(255-color.Red)*m), (byte)(color.Green+(255-color.Green)*m), (byte)(color.Blue+(255-color.Blue)*m));
                hatch.BackgroundColor = color;
                ObjectId hatchid = newbtr.AppendEntity(hatch); // добавляем в модельное пространство

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
                MText mtext = new MText();
                mtext.BackgroundFill = true;
                mtext.UseBackgroundColor = true;
                mtext.BackgroundScaleFactor = 1.1d;
                TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                mtext.TextStyleId =txtstyletable["Standard"];
                mtext.Contents = "TEST_TEXT";
                mtext.TextHeight = 4d;
                mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
                mtext.Location = new Point3d(-2d, -2d, 0d);
                modelspace.AppendEntity(mtext);
                transaction.AddNewlyCreatedDBObject(mtext, true); // и в транзакцию

                transaction.Commit();
            }
        }
    }


    public struct Point
    {
        public double X;
        public double Y;

        public Point(double x, double y)
        {
            X = x; Y = y;
        }
    }
}
