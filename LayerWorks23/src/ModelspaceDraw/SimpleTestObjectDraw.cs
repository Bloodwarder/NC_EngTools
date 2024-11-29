//System

//Modules
using LayerWorks.LayerProcessing;
using LoaderCore.NanocadUtilities;
using Teigha.Colors;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Тестовый класс отрисовки объектов
    /// </summary>
    internal class SimpleTestObjectDraw : LegendObjectDraw
    {
        public SimpleTestObjectDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }

        protected override void CreateEntities()
        {
            Database db = Workstation.Database;
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable? blocktable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;

                BlockTableRecord newbtr = new()
                {
                    Name = "NewBlock",
                    Explodable = true
                };
                blocktable.Add(newbtr);
                transaction.AddNewlyCreatedDBObject(newbtr, true); // и в транзакцию

                //отрезок
                Line line = new(new Point3d(new double[3] { 0d, 0d, 0d }), new Point3d(new double[3] { 2d, 2d, 0d }));
                newbtr.AppendEntity(line); // добавляем в модельное пространство
                transaction.AddNewlyCreatedDBObject(line, true); // и в транзакцию

                //полилиния
                Polyline pl = new();
                pl.AddVertexAt(0, new Point2d(-5d, 0d), 0, 0.2d, 0.2d);
                pl.AddVertexAt(1, new Point2d(-0d, 0d), 1, 0.2d, 0.2d);
                pl.AddVertexAt(2, new Point2d(-0d, 5d), 0, 0.2d, 0.2d);
                pl.SetPointAt(0, new Point2d(-10d, 0d));
                pl.Closed = true;
                ObjectId plid = newbtr.AppendEntity(pl); // добавляем в модельное пространство


                //штриховка
                Hatch hatch = new();
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
                BlockReference bref = new(new Point3d(new double[] { 0d, 5d, 0d }), newbtr.ObjectId);
                modelspace!.AppendEntity(bref);
                transaction.AddNewlyCreatedDBObject(bref, true); // и в транзакцию

                //многострочный текст
                TextStyleTable? txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                MText mtext = new()
                {
                    BackgroundFill = true,
                    UseBackgroundColor = true,
                    BackgroundScaleFactor = 1.1d,
                    TextStyleId = txtstyletable!["Standard"],
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
