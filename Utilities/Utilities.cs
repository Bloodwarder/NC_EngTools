using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace Utilities
{
    /// <summary>
    /// Класс для создания подписей сегментам полилинии
    /// </summary>
    public class Labeler
    {
        private const double LabelTextHeight = 3.6d;
        private const double LabelBackgroundScaleFactor = 1.1d;

        private static string PrevText { get; set; } = "";
        /// <summary>
        /// Создать подпись для сегмента полилинии
        /// </summary>
        [CommandMethod("ПОДПИСЬ")]
        public void LabelDraw()
        {
            Workstation.Define();

            using (Transaction tr = Workstation.TransactionManager.StartTransaction())
            {
                //выбираем полилинию
                PromptEntityOptions peo = new PromptEntityOptions("Выберите полилинию")
                { };
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                if (result.Status != PromptStatus.OK)
                {
                    Workstation.Editor.WriteMessage("Ошибка выбора");
                    return;
                }

                //выбираем точку вставки подписи и находим ближайшую точку на полилинии
                PromptPointOptions ppo = new PromptPointOptions("Укажите точку вставки подписи рядом с сегментом полилинии");
                PromptPointResult pointresult = Workstation.Editor.GetPoint(ppo);
                if (result.Status != PromptStatus.OK)
                    return;
                Point3d point = pointresult.Value;
                Polyline polyline = (Polyline)Workstation.TransactionManager.GetObject(result.ObjectId, OpenMode.ForRead); ;
                Point3d pointonline = polyline.GetClosestPointTo(point, false);

                //проверяем, какому сегменту принадлежит точка и вычисляем его направление
                LineSegment2d ls = new LineSegment2d();
                for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
                {
                    ls = polyline.GetLineSegment2dAt(i);
                    if (SegmentCheck(pointonline.Convert2d(new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1))), ls))
                        break;
                }
                Vector2d v2d = ls.Direction;

                //вводим текст для подписи
                PromptStringOptions pso = new PromptStringOptions($"Введите текст подписи (д или d в начале строки - знак диаметра")
                {
                    AllowSpaces = true,
                    UseDefaultValue = true,
                    DefaultValue = PrevText
                };
                PromptResult pr = Workstation.Editor.GetString(pso);
                if (pr.Status != PromptStatus.OK) return;
                string text = pr.StringResult;
                PrevText = text;

                //ищем таблицу блоков и моделспейс
                BlockTable blocktable = tr.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                BlockTableRecord modelspace = tr.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                //создаём текст
                MText mtext = new MText
                {
                    BackgroundFill = true,
                    UseBackgroundColor = true,
                    BackgroundScaleFactor = LabelBackgroundScaleFactor
                };
                TextStyleTable txtstyletable = tr.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                mtext.TextStyleId = txtstyletable["Standard"];
                mtext.Contents = Regex.Replace(text, "(?<=(^|[1-4]))(д|d)", "%%C"); //заменяем первую букву д на знак диаметра
                mtext.TextHeight = LabelTextHeight;
                mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
                mtext.Location = point;
                mtext.Color = polyline.Color;
                mtext.Layer = polyline.Layer; //устанавливаем цвет и слой как у полилинии
                mtext.Rotation = (v2d.Angle * 180 / Math.PI > 270 || v2d.Angle * 180 / Math.PI < 90) ? v2d.Angle : v2d.Angle + Math.PI; //вычисляем угол поворота с учётом читаемости
                //добавляем в модель и в транзакцию
                modelspace.AppendEntity(mtext);
                tr.AddNewlyCreatedDBObject(mtext, true); // и в транзакцию

                tr.Commit();
            }
        }

        private bool SegmentCheck(Point2d point, LineSegment2d entity)
        {
            Point2d p1 = entity.StartPoint;
            Point2d p2 = entity.EndPoint;

            double maxx = new double[] { p1.X, p2.X }.Max();
            double maxy = new double[] { p1.Y, p2.Y }.Max();
            double minx = new double[] { p1.X, p2.X }.Min();
            double miny = new double[] { p1.Y, p2.Y }.Min();

            if (maxx - minx < 5)
            {
                maxx += 5;
                minx -= 5;
            }
            if (maxy - miny < 5)
            {
                maxy += 5;
                miny -= 5;
            }
            //bool b1 = (point.X>minx & point.X<maxx) & (point.Y>miny & point.Y<maxy);
            return (point.X > minx & point.X < maxx) & (point.Y > miny & point.Y < maxy);
        }

    }

    /// <summary>
    /// Класс команд для вертикальной планировки
    /// </summary>
    public class VerticalCalc
    {
        const string BlackMarkTag = "СУЩ_ОТМ";
        const string RedMarkTag = "КР_ОТМ";
        const string SlopeTag = "УКЛОН";
        const string DistanceTag = "РАССТОЯНИЕ";
        const string WrongEntityErrorString = "Выбран неверный объект. Завершение команды";
        const string ElevationMarkBlockName = "ВП_отметки_блок_241120";
        const string SlopeBlockName = "ВП уклоны блок_041219";

        private static double LastHorStep { get; set; } = 0.2d;

        [CommandMethod("БЛОКТЕСТ")]
        public static void VertTest()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                //запрашиваем у пользователя блок и проверяем выбор на null
                PromptEntityOptions peo = new PromptEntityOptions("Выберите блок с отметкой")
                { AllowNone = false };
                peo.AddAllowedClass(typeof(BlockReference), true);
                PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                if (result.Status != PromptStatus.OK)
                    return;
                BlockReference bref = Workstation.TransactionManager.GetObject(result.ObjectId, OpenMode.ForRead) as BlockReference;
                if (bref == null)
                    return;
                BlockAttributeSet(bref, RedMarkTag, "010.10");

                transaction.Commit();
            }
        }

        [CommandMethod("УКЛОН")]
        public static void SlopeCalc()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты чертежа для расчёта
                if (!TryGetEntity("Выберите блок первой отметки", out BlockReference mark1, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок второй отметки", out BlockReference mark2, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите ось", out Polyline axis))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок уклона", out BlockReference slopeBRef, SlopeBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }


                double red1 = double.Parse(BlockAttributeGet(mark1, RedMarkTag));
                double red2 = double.Parse(BlockAttributeGet(mark2, RedMarkTag));
                double l1 = axis.Length;
                // Расчёт уклона
                double slope = Math.Abs(red2 - red1) / l1 * 1000;
                // Назначение величин блокам
                BlockAttributeSet(slopeBRef, SlopeTag, slope.ToString());
                BlockAttributeSet(slopeBRef, DistanceTag, slope.ToString());
                transaction.Commit();
            }
        }

        [CommandMethod("СЛ_ОТМ")]
        public static void NextMark()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты чертежа для расчёта
                if (!TryGetEntity("Выберите блок первой отметки", out BlockReference mark1, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите ось", out Polyline axis))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок уклона", out BlockReference slopeBRef, SlopeBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                PromptDoubleResult result = Workstation.Editor.GetDouble("Введите уклон в промилле. В случае уклона вниз - введите отрицательное значение");

                double slope;
                if (result.Status == PromptStatus.OK)
                    slope = result.Value;
                else
                    return;
                if (!TryGetEntity("Выберите блок для расчёта отметки", out BlockReference markNext, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }



                double red1 = double.Parse(BlockAttributeGet(mark1, RedMarkTag));
                double l1 = axis.Length;

                double redNext = red1 + l1 * slope * 0.001d;

                BlockAttributeSet(slopeBRef, SlopeTag, slope.ToString());
                BlockAttributeSet(slopeBRef, DistanceTag, l1.ToString());
                BlockAttributeSet(markNext, RedMarkTag, redNext.ToString());

                transaction.Commit();
            }
        }

        [CommandMethod("СР_ОТМ")]
        public static void AverageLevel()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты чертежа для расчёта
                if (!TryGetEntity("Выберите блок первой (нижней) отметки", out BlockReference mark1, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок второй (верхней) отметки", out BlockReference mark2, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок рассчитываемой отметки", out BlockReference markOutput, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите ось 1", out Polyline axis1))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите ось 2", out Polyline axis2))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок уклона", out BlockReference slopeBRef, SlopeBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }



                // Получить значения для расчёта
                double red1 = double.Parse(BlockAttributeGet(mark1, RedMarkTag));
                double red2 = double.Parse(BlockAttributeGet(mark2, RedMarkTag));
                double l1 = axis1.Length;
                double l2 = axis2.Length;

                // Расчёт
                double red3 = red1 + Math.Abs(red2 - red1) * l1 / (l1 + l2);

                // Назначить аттрибут блока
                BlockAttributeSet(markOutput, RedMarkTag, red3.ToString());
                transaction.Commit();
            }
        }

        [CommandMethod("ГОРИЗ_РАСЧ")]
        public static void HorizontalCalc()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                if (!TryGetEntity("Выберите блок первой отметки", out BlockReference mark1, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите блок второй отметки", out BlockReference mark2, ElevationMarkBlockName))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                if (!TryGetEntity("Выберите ось", out Polyline axis))
                { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                PromptDoubleResult result = Workstation.Editor.GetDistance("Укажите полуширину проезжей части");
                if (result.Status != PromptStatus.OK)
                {
                    Workstation.Editor.WriteMessage("Неверный ввод");
                    return;
                }
                double halfWidth = result.Value;
                PromptDoubleOptions pdo = new PromptDoubleOptions($"Укажите шаг горизонталей <{LastHorStep}>")
                {
                    UseDefaultValue = true,
                    DefaultValue = LastHorStep
                };
                PromptDoubleResult result2 = Workstation.Editor.GetDouble(pdo);
                if (result.Status != PromptStatus.OK)
                {
                    Workstation.Editor.WriteMessage("Неверный ввод");
                    return;
                }
                double horStep = Math.Min(Math.Max(result2.Value, 0.1d), 5d);
                LastHorStep = horStep;




                // Получить значения для расчёта
                double red1 = double.Parse(BlockAttributeGet(mark1, RedMarkTag));
                double red2 = double.Parse(BlockAttributeGet(mark2, RedMarkTag));
                double l1 = axis.Length;

                bool upwards = red2 > red1;
                double slope = Math.Abs((red2 - red1) / l1);
                double axisStep = horStep / slope;

                double scaleDifference = Math.Round(red1 % 1, 2) * 100;
                double horStep100 = horStep * 100;
                while (scaleDifference < horStep100)
                {
                    scaleDifference -= horStep100;
                }

                double levelDisplacement = upwards ? horStep100 - scaleDifference : scaleDifference;
                double axisDisplacement = levelDisplacement / slope;

                Workstation.Editor.WriteMessage($"\nУклон: {string.Format("0", slope * 1000)}");
                Workstation.Editor.WriteMessage($"\nШаг на оси: {string.Format("0.0", axisStep)}");
                Workstation.Editor.WriteMessage($"\nСмещение на оси от первой отметки: {string.Format("0.0", axisDisplacement)}");


                //'ВСТАВКА ТЕКСТА С ПАРАМЕТРАМИ (геометрия)
                //Set ResTxt = acd.ModelSpace.AddMText(blc1.InsertionPoint, 0, "Уклон: " + Format(Slp * 1000, "0") + Chr(10) + "Шаг на оси: " + Format(AxStep, "0.0") _
                // + Chr(10) + "Смещение на оси от первой отметки: " + Format(AxDispl, "0.0"))
                //txtclr.ColorIndex = acRed
                //With ResTxt
                //    .BackgroundFill = False
                //    .AttachmentPoint = acAttachmentPointTopLeft
                //    .TrueColor = txtclr
                //    .Height = 3.5
                //    .EntityTransparency = 0
                //End With

                //vy = blc1.InsertionPoint(1) - blc2.InsertionPoint(1)
                //vx = blc1.InsertionPoint(0) - blc2.InsertionPoint(0)

                //If vx > 0 Then
                //    With ResTxt
                //        .AttachmentPoint = acAttachmentPointTopRight
                //        .InsertionPoint = blc1.InsertionPoint
                //    End With
                //End If

                //ResTxt.Rotation = Atn(vy / vx)

                transaction.Commit();
            }
        }

        [CommandMethod("КРАСН_ЧЕРН_УРАВН")]
        public static void RedBlackEqual()
        {
            PromptSelectionResult result = Workstation.Editor.SelectImplied();
            SelectionSet selectionSet;
            if (result.Status == PromptStatus.OK)
                selectionSet = result.Value;
            else
                return;
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {

                List<Entity> entities = (from ObjectId id in selectionSet
                                         let entity = transaction.GetObject(id, OpenMode.ForRead) as Entity
                                         where entity is BlockReference blockReference && blockReference.BlockName == ElevationMarkBlockName
                                         select entity).ToList();
                foreach (Entity entity in entities)
                {
                    double elevation = double.Parse(BlockAttributeGet((BlockReference)entity, BlackMarkTag));
                    BlockAttributeSet((BlockReference)entity, RedMarkTag, elevation.ToString());
                }
                transaction.Commit();
            }
        }

        // Служебные приватные методы
        private static string BlockAttributeGet(BlockReference bref, string tag)
        {
            AttributeCollection atrs = bref.AttributeCollection;
            var atrref = (from ObjectId objid in atrs
                          let rfr = Workstation.TransactionManager.TopTransaction.GetObject(objid, OpenMode.ForRead) as AttributeReference
                          where rfr.Tag == tag
                          select rfr).FirstOrDefault();
            if (atrref != null)
            {
                return atrref.TextString ?? "";
            }
            else
            {
                return string.Empty;
            }
        }

        private static void BlockAttributeSet(BlockReference bref, string tag, string value)
        {
            AttributeCollection atrs = bref.AttributeCollection;
            var atrref = (from ObjectId objid in atrs
                          let rfr = Workstation.TransactionManager.TopTransaction.GetObject(objid, OpenMode.ForRead) as AttributeReference
                          where rfr.Tag == tag
                          select rfr).FirstOrDefault();
            if (atrref == null) return;
            atrref.TextString = value;
        }

        private static bool TryGetEntity<T>(string message, out T entity, string blockName = null) where T : Entity
        {
            PromptEntityOptions peo = new PromptEntityOptions(message)
            { AllowNone = false };
            peo.AddAllowedClass(typeof(T), true);
            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
            {
                entity = null;
                return false;
            }
            entity = Workstation.TransactionManager.GetObject(result.ObjectId, OpenMode.ForRead) as T;

            if (blockName != null)
            {
                if (entity is BlockReference blockReference && blockReference.BlockName != blockName)
                {
                    entity = null;
                    return false;
                }
            }
            return true;
        }
    }

    internal static class Workstation
    {
        private static Document document;
        private static Database database;
        private static Teigha.DatabaseServices.TransactionManager transactionManager;
        private static Editor editor;


        public static Document Document => document;
        public static Database Database => database;
        public static Teigha.DatabaseServices.TransactionManager TransactionManager => transactionManager;
        public static Editor Editor => editor;

        public static void Define()
        {
            document = Application.DocumentManager.MdiActiveDocument;
            database = HostApplicationServices.WorkingDatabase;
            transactionManager = Database.TransactionManager;
            editor = Document.Editor;
        }

    }
}
