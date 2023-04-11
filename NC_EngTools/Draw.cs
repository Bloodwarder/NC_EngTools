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

namespace ModelspaceDraw
{
    internal abstract class ObjectDraw
    {
        internal List<Entity> EntitiesList { get; } = new List<Entity>();
        private protected Point2d _basepoint;
        private protected RecordLayerParser _layer;
        internal ObjectDraw(Point2d basepoint, RecordLayerParser layer = null)
        {
            _basepoint = basepoint;
            _layer = layer;
        }
        internal abstract void Draw();
        private protected Point2d relativePoint(double x, double y)
        {
            return new Point2d(x+_basepoint.X, y+_basepoint.Y);
        }
    }
    internal abstract class LegendObjectDraw : ObjectDraw
    {
        internal LegendObjectDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal static double CellWidth { get; set; }
        internal static double CellHeight { get; set; }
    }

    internal class SolidLineDraw : LegendObjectDraw
    {
        public SolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        internal override void Draw()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, relativePoint(-CellWidth/2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, relativePoint(CellWidth/2, 0d), 0, 0d, 0d);
            pl.Layer = _layer.OutputLayerName;
            LayerProps lp = LayerPropertiesDictionary.GetValue(_layer.TrueName, out bool success);
            if (success)
            {
                pl.LinetypeScale=lp.LTScale;
                pl.ConstantWidth=lp.ConstantWidth;
            }
            EntitiesList.Add(pl);
        }
    }

    internal abstract class MarkedLineDraw : LegendObjectDraw
    {
        internal string MarkChar { get; set; }
        private double _width;

        internal MarkedLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
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
            mtext.TextHeight = 4d;
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
            mtext.Location = new Point3d(_basepoint.X, _basepoint.Y, 0d);
            mtext.Layer = _layer.BoundLayer.Name;
            _width = mtext.ActualWidth;
            EntitiesList.Add(mtext);

        }
        private protected List<Polyline> drawLines()
        {
            Polyline pl1 = new Polyline();
            Polyline pl2 = new Polyline();
            pl1.AddVertexAt(0, relativePoint(-CellWidth/2, 0d), 0, 0d, 0d);
            pl1.AddVertexAt(1, relativePoint(-(_width/2+0.5d), 0d), 0, 0d, 0d);
            pl1.Layer = _layer.BoundLayer.Name;

            pl2.AddVertexAt(0, relativePoint(_width/2+0.5d, 0d), 0, 0d, 0d);
            pl2.AddVertexAt(1, relativePoint(CellWidth/2, 0d), 0, 0d, 0d);
            pl2.Layer = _layer.BoundLayer.Name;

            List<Polyline> list = new List<Polyline> { pl1, pl2 };
            EntitiesList.AddRange(list);
            return list;
        }
        protected abstract void formatLines(IEnumerable<Polyline> lines);

    }
    internal class MarkedSolidLineDraw : MarkedLineDraw
    {
        public MarkedSolidLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        protected override void formatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(_layer, out _, true).ConstantWidth;
                line.LinetypeId = SymbolUtilityServices.GetLinetypeContinuousId(Workstation.Database);
            }
        }
    }
    internal class MarkedDashedLineDraw : MarkedLineDraw
    {
        public MarkedDashedLineDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        protected override void formatLines(IEnumerable<Polyline> lines)
        {
            foreach (Polyline line in lines)
            {
                line.ConstantWidth = LayerPropertiesDictionary.GetValue(_layer, out _, true).ConstantWidth;
                LayerChecker.CheckLinetype("ACAD_ISO02W100", out bool ltgetsuccess);
                if (ltgetsuccess)
                    line.Linetype = "ACAD_ISO02W100";
                line.LinetypeScale=0.3d;
            }
        }
    }

    internal abstract class AreaDraw : LegendObjectDraw
    {
        internal AreaDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        private protected void drawHatch(IEnumerable<Polyline> borders, string patternname = "SOLID", double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8, Color background = null)
        {
            Hatch hatch = new Hatch();
            hatch.SetHatchPattern(HatchPatternType.UserDefined, patternname);
            hatch.HatchStyle = HatchStyle.Normal;
            hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(borders.Select(o => o.ObjectId).ToArray()));
            hatch.PatternAngle = angle *Math.PI/180;
            hatch.PatternScale = patternscale;
            Color color = _layer.BoundLayer.Color;
            color = Color.FromRgb((byte)(color.Red+(255-color.Red)*increasebrightness), (byte)(color.Green+(255-color.Green)*increasebrightness), (byte)(color.Blue+(255-color.Blue)*increasebrightness));
            hatch.BackgroundColor = background;
            EntitiesList.Add(hatch);
        }
    }

    internal class RectangleDraw : AreaDraw
    {
        public RectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        internal double RectangleWidth { get; set; }
        internal double RectangleHeight { get; set; }


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
            rectangle.Layer = layer == null ? _layer.BoundLayer.Name : layer;
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

    internal class HatchedRectangleDraw : RectangleDraw
    {
        public HatchedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

        internal override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangle);
        }
    }

    internal class HatchedCircleDraw : AreaDraw
    {
        internal double Radius { get; set; }
        public HatchedCircleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }

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
            circle.Layer = layer==null ? _layer.BoundLayer.Name : layer;
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

    internal class FencedRectangleDraw : RectangleDraw
    {
        string FenceLayer { get; set; } = null;
        internal double FenceWidth { get; set; }
        internal double FenceHeight { get; set; }
        public FencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal override void Draw()
        {
            List<Polyline> rectangle = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangle);
            drawRectangle(FenceWidth, FenceHeight, FenceLayer);
        }
    }

    internal class HatchedFencedRectangleDraw : RectangleDraw
    {
        string FenceLayer { get; set; } = null;
        internal double FenceWidth { get; set; }
        internal double FenceHeight { get; set; }
        public HatchedFencedRectangleDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal override void Draw()
        {
            List<Polyline> rectangles = new List<Polyline> { drawRectangle(RectangleWidth, RectangleHeight) };
            drawHatch(rectangles);
            rectangles.Add(drawRectangle(FenceWidth, FenceHeight, FenceLayer));
            drawHatch(rectangles);
        }
    }

    internal class BlockReferenceDraw : LegendObjectDraw
    {
        internal string BlockName { get; set; }
        internal string FilePath { get; set; }
        BlockTable _blocktable;
        public BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
            _blocktable = Workstation.TransactionManager.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        }

        internal override void Draw()
        {
            BlockReference bref = new BlockReference(new Point3d(_basepoint.X, _basepoint.Y, 0d), findBlockTableRecord(BlockName));
            EntitiesList.Add(bref);
        }

        private ObjectId findBlockTableRecord(string blockname)
        {
            var blocktablerecordid = from ObjectId elem in _blocktable
                                     let btr = Workstation.TransactionManager.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTableRecord
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
                TextStyleTable txtstyletable = transaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
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
