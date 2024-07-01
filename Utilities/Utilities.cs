using HostMgd.EditorInput;
using LoaderCore.Configuration;
using NanocadUtilities;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using static Utilities.EntitySelector;

namespace Utilities
{
    /// <summary>
    /// Класс для создания подписей сегментам полилинии
    /// </summary>
    public class Labeler
    {
        private const double DefaultLabelTextHeight = 3.6d;
        private const double DefaultLabelBackgroundScaleFactor = 1.1d;

        static Labeler()
        {
            var u = Configuration.Utilities;
            LabelTextHeight = u?.DefaultLabelTextHeight ?? DefaultLabelTextHeight;
            LabelBackgroundScaleFactor = u?.DefaultLabelBackgroundScaleFactor ?? DefaultLabelBackgroundScaleFactor;
        }

        private static double LabelTextHeight { get; set; }
        private static double LabelBackgroundScaleFactor { get; set; }
        private static string PrevText { get; set; } = "";
        /// <summary>
        /// Создать подпись для сегмента полилинии
        /// </summary>
        [CommandMethod("ПОДПИСЬ")]
        public void LabelDraw()
        {
            Workstation.Define();
            try
            {
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
                    Polyline polyline = (Polyline)tr.GetObject(result.ObjectId, OpenMode.ForRead);

                    Vector2d v2d = GetPolylineSegment(polyline!, point).Direction;

                    //вводим текст для подписи
                    PromptStringOptions pso = new($"Введите текст подписи (д или d в начале строки - знак диаметра")
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
                    BlockTable? blocktable = tr.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                    BlockTableRecord? modelspace = tr.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    //создаём текст
                    MText mtext = new()
                    {
                        BackgroundFill = true,
                        UseBackgroundColor = true,
                        BackgroundScaleFactor = LabelBackgroundScaleFactor
                    };
                    TextStyleTable txtstyletable = (TextStyleTable)tr.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead);
                    mtext.TextStyleId = txtstyletable!["Standard"];
                    mtext.Contents = Regex.Replace(text, "(?<=(^|[1-4]))(д|d)", "%%C"); //заменяем первую букву д на знак диаметра
                    mtext.TextHeight = LabelTextHeight;
                    mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
                    mtext.Location = point;
                    mtext.Color = polyline.Color;
                    mtext.Layer = polyline.Layer; //устанавливаем цвет и слой как у полилинии
                    mtext.Rotation = CalculateTextOrient(v2d); //вычисляем угол поворота с учётом читаемости
                                                               //добавляем в модель и в транзакцию
                    modelspace!.AppendEntity(mtext);
                    tr.AddNewlyCreatedDBObject(mtext, true); // и в транзакцию

                    tr.Commit();
                }
            }
            finally
            {
                Highlighter.Unhighlight();
            }
        }

        [CommandMethod("ТОРИЕНТ")]
        public static void TextOrientBy2Points()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetEntity("Выберите МТекст", out MText? mText))
                    { Workstation.Editor.WriteMessage("Ошибка выбора"); return; }

                    //выбираем точку вставки подписи и находим ближайшую точку на полилинии
                    PromptPointOptions ppo = new PromptPointOptions("Укажите первую точку")
                    {
                        UseBasePoint = false
                    };
                    PromptPointResult pointresult = Workstation.Editor.GetPoint(ppo);
                    if (pointresult.Status != PromptStatus.OK)
                        return;
                    Point3d point1 = pointresult.Value;
                    PromptAngleOptions pao = new("Укажите угол")
                    {
                        UseBasePoint = true,
                        BasePoint = point1
                    };
                    PromptDoubleResult angleresult = Workstation.Editor.GetAngle(pao);
                    if (angleresult.Status != PromptStatus.OK)
                        return;
                    mText!.Rotation = angleresult.Value;
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }


        }

        [CommandMethod("ПЛТОРИЕНТ")]
        public void TextOrientByPolilineSegment()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetEntity("Выберите МТекст", out MText? mText))
                    { Workstation.Editor.WriteMessage("Ошибка выбора"); return; }

                    PromptEntityOptions peo = new PromptEntityOptions("Выберите полилинию")
                    { };
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                    if (result.Status != PromptStatus.OK)
                    {
                        Workstation.Editor.WriteMessage("Ошибка выбора");
                        return;
                    }
                    Polyline polyline = (Polyline)transaction.GetObject(result.ObjectId, OpenMode.ForRead);
                    LineSegment2d segment = GetPolylineSegment(polyline, result.PickedPoint);
                    mText!.Rotation = CalculateTextOrient(segment.Direction);
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
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

        private double CalculateTextOrient(Vector2d v2d)
        {
            return (v2d.Angle * 180 / Math.PI > 270 || v2d.Angle * 180 / Math.PI < 90) ? v2d.Angle : v2d.Angle + Math.PI;
        }

        private LineSegment2d GetPolylineSegment(Polyline polyline, Point3d point)
        {
            Point3d pointonline = polyline.GetClosestPointTo(point, false);

            //проверяем, какому сегменту принадлежит точка и вычисляем его направление
            LineSegment2d ls = new LineSegment2d();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                ls = polyline.GetLineSegment2dAt(i);
                if (SegmentCheck(pointonline.Convert2d(new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1))), ls))
                    break;
            }
            return ls;
        }
    }

    /// <summary>
    /// Класс команд для работы с текстом (мтекстом)
    /// </summary>
    public static class TextProcessor
    {
        /// <summary>
        /// Убирает форматирование из МТекста, если оно задано не стилем, а внутри текста
        /// </summary>
        [CommandMethod("СМТ", CommandFlags.UsePickSet)]
        public static void StripMText()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new PromptSelectionOptions()
                { };
                PromptSelectionResult result = Workstation.Editor.GetSelection(pso);
                if (result.Status != PromptStatus.OK)
                    return;
                var texts = from ObjectId id in result.Value.GetObjectIds()
                            let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                            where entity is MText
                            select entity as MText;
                // если текст заключен в шаблон для форматирования ({/ ... ; ТЕКСТ }), оставить только чистый текст (ТЕКСТ)
                Regex regex = new Regex(@"(?<=(^{(\\.*;)+\b)).+(?=(\b}$))");
                foreach (var text in texts)
                {
                    var matches = regex.Matches(text.Contents);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            match.Result(match.Value);
                        }
                    }
                }
                transaction.Commit();
                // Пока работает только на целиком отформатированный текст
            }
        }

        /// <summary>
        /// Назначает тексту нулевую ширину для удобства автоматической обработки выравнивания и фона
        /// </summary>
        [CommandMethod("НУЛЕВАЯШИРИНАТЕКСТА")]
        public static void AssignZeroWidth()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new PromptSelectionOptions()
                { };
                PromptSelectionResult result = Workstation.Editor.GetSelection(pso);
                if (result.Status != PromptStatus.OK)
                    return;
                var texts = from ObjectId id in result.Value.GetObjectIds()
                            let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                            where entity is MText
                            select entity as MText;
                foreach (var text in texts)
                {
                    text.Width = 0d;
                }
                transaction.Commit();
            }
        }
    }

    public class EntityPointPolylineTracer
    {
        [CommandMethod("ОБЪЕКТСОЕД")]
        public void TracePolyline()
        {
            Workstation.Define();
            // Объявить переменную для сохранения id создаваемой полилинии
            ObjectId polylineId;
            // Получаем первые 2 объекта и создаём полилинию из двух точек
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Polyline polyline = new Polyline
                {
                    LayerId = Workstation.Database.Clayer
                };
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        Point2d p = GetEntityBasePoint();
                        polyline.AddVertexAt(polyline.NumberOfVertices, p, 0, 0d, 0d);
                    }
                    catch (CancelledByUserException)
                    {
                        transaction.Abort();
                        return;
                    }
                }
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                modelspace!.AppendEntity(polyline);
                polylineId = polyline.Id;
                transaction.Commit();
            }

            // Получаем от пользователя объекты и добавляем по ним точки в полилинию до завершения команды
            while (true)
            {
                try
                {
                    using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
                    {
                        Polyline polyline = (Polyline)transaction.GetObject(polylineId, OpenMode.ForWrite);
                        Point2d p = GetEntityBasePoint();
                        polyline.AddVertexAt(polyline.NumberOfVertices, p, 0, 0d, 0d);
                        transaction.Commit();
                    }
                }
                catch (CancelledByUserException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Получает характерную точку объекта
        /// </summary>
        /// <returns>Характерная точка объекта (как правило, центр или точка вставки)</returns>
        /// <exception cref="CancelledByUserException">Срабатывает когда пользователь завершает команду</exception>
        private static Point2d GetEntityBasePoint()
        {
            PromptEntityOptions peo = new PromptEntityOptions("Выберите объект");
            peo.AddAllowedClass(typeof(BlockReference), true);
            peo.AddAllowedClass(typeof(Circle), true);

            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
                throw new CancelledByUserException();
            Entity entity = (Entity)Workstation.TransactionManager.TopTransaction.GetObject(result.ObjectId, OpenMode.ForRead);

            if (entity is BlockReference bref)
            {
                Point3d p = bref.Position;
                return new Point2d(p.X, p.Y);
            }
            else if (entity is Circle circle)
            {
                Point3d p = circle.Center;
                return new Point2d(p.X, p.Y);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal class CancelledByUserException : System.Exception
        {
        }
    }

    /// <summary>
    /// Конвертер мультилиний в полилинии
    /// </summary>
    public class MultilineConverter
    {
        /// <summary>
        /// Получить мультилинии с чертежа и конвертировать их в полилинии
        /// </summary>
        [CommandMethod("МЛИНВПЛИН", CommandFlags.UsePickSet)]
        public void ConvertMultilineToPolyline()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // Получить объекты и отфильтровать мультилинии
                PromptSelectionResult psr = Workstation.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                    return;
                List<Mline> mlines = psr.Value.GetObjectIds()
                                             .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForWrite))
                                             .Where(e => e is Mline)
                                             .Select(e => (Mline)e)
                                             .ToList();
                // Создать построить полилинии по вершинам каждой мультилинии и добавить в новую коллекцию
                List<Polyline> polylines = new List<Polyline>();
                foreach (Mline multiline in mlines)
                {
                    Polyline polyline = new Polyline
                    {
                        Layer = multiline.Layer
                    };
                    for (int i = 0; i < multiline.NumberOfVertices; i++)
                    {
                        Point3d p = multiline.VertexAt(i);
                        polyline.AddVertexAt(i, new Point2d(p.X, p.Y), 0, 0d, 0d);
                    }
                    polylines.Add(polyline);
                }

                // Добавить в чертёж полилинии и удалить мультилинии
                BlockTable? blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                BlockTableRecord? modelspace = transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;

                foreach (Polyline polyline in polylines)
                    modelspace!.AppendEntity(polyline);
                foreach (Mline multiline in mlines)
                    multiline.Erase();
                transaction.Commit();
            }
        }
    }
    /// <summary>
    /// Класс команд для вертикальной планировки
    /// </summary>
    public class VerticalCalc
    {
        const string WrongEntityErrorString = "Выбран неверный объект. Завершение команды";

        static VerticalCalc()
        {
            var v = Configuration.Utilities.Vertical;

            BlackMarkTag = v.BlackMarkTag ?? "СУЩ_ОТМ";
            RedMarkTag = v.RedMarkTag ?? "КР_ОТМ";
            SlopeTag = v.SlopeTag ?? "УКЛОН";
            DistanceTag = v.DistanceTag ?? "РАССТОЯНИЕ";
            ElevationMarkBlockName = v.ElevationMarkBlockName ?? "ВП_отметки_блок_241120";
            SlopeBlockName = v.SlopeBlockName ?? "ВП уклоны блок_041219";

        }

        private static string BlackMarkTag { get; set; }
        private static string RedMarkTag { get; set; }
        private static string SlopeTag { get; set; }
        private static string DistanceTag { get; set; }
        private static string ElevationMarkBlockName { get; set; }
        private static string SlopeBlockName { get; set; }

        private static double LastHorStep { get; set; } = 0.2d;

        [CommandMethod("УКЛОН")]
        public static void SlopeCalc()
        {
            Workstation.Define();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try

                {
                    // Получить объекты чертежа для расчёта
                    if (!TryGetEntity("Выберите блок первой отметки", out BlockReference? mark1, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок второй отметки", out BlockReference? mark2, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось", out Polyline? axis))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок уклона", out BlockReference? slopeBRef, SlopeBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }


                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag));
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag));
                    double l1 = axis!.Length;
                    // Расчёт уклона
                    double slope = Math.Abs(red2 - red1) / l1 * 1000;
                    // Назначение величин блокам
                    SetBlockAttribute(slopeBRef!, SlopeTag, slope.ToString("0"));
                    SetBlockAttribute(slopeBRef!, DistanceTag, l1.ToString("0.0"));
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }
        }

        [CommandMethod("СЛ_ОТМ")]
        public static void NextMark()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    // Получить объекты чертежа для расчёта
                    if (!TryGetEntity("Выберите блок первой отметки", out BlockReference? mark1, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось", out Polyline? axis))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок уклона", out BlockReference? slopeBRef, SlopeBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    PromptDoubleResult result = Workstation.Editor.GetDouble("Введите уклон в промилле. В случае уклона вниз - введите отрицательное значение");

                    double slope;
                    if (result.Status == PromptStatus.OK)
                        slope = result.Value;
                    else
                        return;
                    if (!TryGetEntity("Выберите блок для расчёта отметки", out BlockReference? markNext, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }



                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag));
                    double l1 = axis!.Length;

                    double redNext = red1 + l1 * slope * 0.001d;

                    SetBlockAttribute(slopeBRef!, SlopeTag, Math.Abs(slope).ToString("0"));
                    SetBlockAttribute(slopeBRef!, DistanceTag, l1.ToString("0.0"));
                    SetBlockAttribute(markNext!, RedMarkTag, redNext.ToString("0.00"));
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }
        }

        [CommandMethod("СР_ОТМ")]
        public static void AverageLevel()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    // Получить объекты чертежа для расчёта
                    if (!TryGetEntity("Выберите блок первой (нижней) отметки", out BlockReference? mark1, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок второй (верхней) отметки", out BlockReference? mark2, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок рассчитываемой отметки", out BlockReference? markOutput, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось 1", out Polyline? axis1))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось 2", out Polyline? axis2))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }

                    // Получить значения для расчёта
                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag));
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag));
                    double l1 = axis1!.Length;
                    double l2 = axis2!.Length;

                    // Расчёт
                    double red3 = red1 + Math.Abs(red2 - red1) * l1 / (l1 + l2);

                    // Назначить аттрибут блока
                    SetBlockAttribute(markOutput!, RedMarkTag, red3.ToString("0.00"));
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }
        }

        [CommandMethod("ГОРИЗ_РАСЧ")]
        public static void HorizontalCalc()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetEntity("Выберите блок первой отметки", out BlockReference? mark1, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок второй отметки", out BlockReference? mark2, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось", out Polyline? axis))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }

                    PromptDoubleOptions pdo = new($"Укажите шаг горизонталей ")
                    {
                        UseDefaultValue = true,
                        DefaultValue = LastHorStep
                    };
                    PromptDoubleResult result2 = Workstation.Editor.GetDouble(pdo);
                    if (result2.Status != PromptStatus.OK)
                    {
                        Workstation.Editor.WriteMessage("Неверный ввод");
                        return;
                    }
                    double horStep = Math.Min(Math.Max(result2.Value, 0.1d), 5d);
                    LastHorStep = horStep;

                    // Получить значения для расчёта
                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag));
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag));
                    double l1 = axis!.Length;

                    bool upwards = red2 > red1;
                    double slope = Math.Abs((red2 - red1) / l1);
                    double axisStep = horStep / slope;

                    double scaleDifference = Math.Round(red1 % 1, 2) * 100;
                    double horStep100 = horStep * 100;
                    scaleDifference %= horStep100;

                    double levelDisplacement = upwards ? horStep100 - scaleDifference : scaleDifference;
                    double axisDisplacement = levelDisplacement * 0.01d / slope;

                    StringBuilder sb = new();
                    sb.Append($"\nУклон: {slope * 1000d:0}");
                    sb.Append($"\nШаг на оси: {axisStep:0.0}");
                    sb.Append($"\nСмещение на оси от первой отметки: {axisDisplacement:0.0}");
                    string textContent = sb.ToString();

                    Workstation.Editor.WriteMessage(textContent);

                    BlockTable? blockTable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord? modelSpace = transaction.GetObject(blockTable![BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                    double vx = mark1!.Position.X - mark2!.Position.X;
                    double vy = mark1.Position.Y - mark2.Position.Y;
                    MText mText = new()
                    {
                        BackgroundFill = false,
                        Attachment = vx > 0 ? AttachmentPoint.TopRight : AttachmentPoint.TopLeft,
                        Color = Color.FromRgb(0, 0, 255),
                        TextHeight = 3.5d,
                        Location = mark1.Position,
                        Rotation = Math.Atan(vy / vx),
                        Contents = textContent,
                    };
                    modelSpace!.AppendEntity(mText);
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }

        }

        [CommandMethod("КРАСН_ЧЕРН_УРАВН", CommandFlags.Redraw)]
        public static void RedBlackEqual()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionResult result = Workstation.Editor.SelectImplied();
                SelectionSet selectionSet;
                if (result.Status == PromptStatus.OK)
                    selectionSet = result.Value;
                else
                    return;
                List<Entity> entities = (from ObjectId id in selectionSet.GetObjectIds()
                                         let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                                         where entity is BlockReference blockReference && blockReference.BlockTableRecordName() == ElevationMarkBlockName
                                         select entity).ToList();
                foreach (Entity entity in entities)
                {
                    double elevation = double.Parse(GetBlockAttribute((BlockReference)entity, BlackMarkTag));
                    SetBlockAttribute((BlockReference)entity, RedMarkTag, elevation.ToString());
                }
                transaction.Commit();
            }
        }

        // Служебные приватные методы
        private static string GetBlockAttribute(BlockReference bref, string tag)
        {
            AttributeCollection atrs = bref.AttributeCollection;
            var atrref = (from ObjectId objid in atrs
                          let rfr = Workstation.TransactionManager.TopTransaction.GetObject(objid, OpenMode.ForRead) as AttributeReference
                          where rfr.Tag == tag
                          select rfr).FirstOrDefault();
            if (atrref != null)
            {
                return atrref.TextString.Replace(".", ",") ?? "";
            }
            else
            {
                return string.Empty;
            }
        }

        private static void SetBlockAttribute(BlockReference bref, string tag, string value)
        {
            AttributeCollection atrs = bref.AttributeCollection;
            var atrref = (from ObjectId objid in atrs
                          let rfr = Workstation.TransactionManager.TopTransaction.GetObject(objid, OpenMode.ForWrite) as AttributeReference
                          where rfr.Tag == tag
                          select rfr).FirstOrDefault();
            if (atrref == null) return;
            atrref.TextString = value.Replace(",", ".");
        }
    }

    internal static class BlockReferenceNameExtension
    {
        internal static string BlockTableRecordName(this BlockReference bref)
        {
            if (bref.IsDynamicBlock)
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.DynamicBlockTableRecord, OpenMode.ForRead);
                return btr!.Name;
            }
            else
            {
                BlockTableRecord btr = (BlockTableRecord)Workstation.TransactionManager.TopTransaction.GetObject(bref.BlockTableRecord, OpenMode.ForRead);
                return btr!.Name;
            }
        }
    }

    internal static class EntitySelector
    {
        internal static bool TryGetEntity<T>(string message, out T? entity, string? blockName = null) where T : Entity
        {
            PromptEntityOptions peo = new(message)
            { AllowNone = false };
            peo.AddAllowedClass(typeof(T), true);
            peo.SetRejectMessage("Неверный тип объекта");
            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
            {
                entity = null;
                return false;
            }
            entity = (T)Workstation.TransactionManager.TopTransaction.GetObject(result.ObjectId, OpenMode.ForWrite);
            Highlighter.Highlight(entity);
            if (blockName != null)
            {
                if (entity is BlockReference blockReference && blockReference.BlockTableRecordName() != blockName)
                {
                    entity = null;
                    return false;
                }
            }
            return true;
        }
    }
    internal static class Highlighter
    {
        private readonly static List<DBObjectWrapper<Entity>> _list = new();


        internal static void Highlight(Entity entity)
        {
            _list.Add(new DBObjectWrapper<Entity>(entity, OpenMode.ForWrite));
            entity.Highlight();
        }

        internal static void Unhighlight()
        {
            try
            {
                foreach (DBObjectWrapper<Entity> entity in _list)
                    entity.Get().Unhighlight();
            }
            finally
            {
                _list.Clear();
            }
        }
    }

}
