using HostMgd.EditorInput;
using LoaderCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LoaderCore.NanocadUtilities;
using System.Text.RegularExpressions;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;
using static Utilities.EntitySelector;

namespace Utilities
{
    /// <summary>
    /// Класс для создания подписей сегментам полилинии
    /// </summary>
    public static class Labeler
    {
        private const double DefaultLabelTextHeight = 3.6d;
        private const double DefaultLabelBackgroundScaleFactor = 1.1d;

        static Labeler()
        {
            IConfigurationSection? configuration = NcetCore.ServiceProvider.GetService<IConfiguration>()?.GetSection("UtilitiesConfiguration");
            double? textHeight = configuration?.GetValue<double>("DefaultLabelTextHeight");
            double? backgroundScaleFactor = configuration?.GetValue<double>("DefaultLabelBackgroundScaleFactor");
            LabelTextHeight = textHeight ?? DefaultLabelTextHeight;
            LabelBackgroundScaleFactor = backgroundScaleFactor ?? DefaultLabelBackgroundScaleFactor;
        }

        private static double LabelTextHeight { get; set; }
        private static double LabelBackgroundScaleFactor { get; set; }
        private static string PrevText { get; set; } = "";
        /// <summary>
        /// Создать подпись для сегмента полилинии
        /// </summary>
        [CommandMethod("ПОДПИСЬ")]
        public static void LabelDraw()
        {
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
                    PromptStringOptions pso = new($"Введите текст подписи (д или d в начале строки - знак диаметра) [Высота/Фон]:")
                    {
                        AllowSpaces = true,
                        UseDefaultValue = true,
                        DefaultValue = PrevText
                    };
                    PromptResult pr = Workstation.Editor.GetString(pso);
                    while (pr.Status == PromptStatus.Keyword)
                    {
                        switch (pr.StringResult)
                        {
                            case "Высота":
                                AssignNewTextHeight();
                                break;
                            case "Фон":
                                AssignBackgroundFactor();
                                break;
                        }
                        pr = Workstation.Editor.GetString(pso);
                    }
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

        private static void AssignBackgroundFactor()
        {
            PromptDoubleResult result = Workstation.Editor.GetDouble("Введите новый коэффициент фона текста:");
            if (result.Status != PromptStatus.OK)
                Workstation.Editor.WriteMessage("Неверное значение");
            double newValue = Math.Clamp(result.Value, 1d, 2d);
            LabelBackgroundScaleFactor = newValue;
            // TODO: обновить в файле конфигурации
        }

        private static void AssignNewTextHeight()
        {
            PromptDoubleResult result = Workstation.Editor.GetDouble("Введите новый коэффициент фона текста:");
            if (result.Status != PromptStatus.OK)
                Workstation.Editor.WriteMessage("Неверное значение");
            double newValue = Math.Max(result.Value, 0.01d);
            LabelTextHeight = newValue;
            // TODO: обновить в файле конфигурации
        }

        [CommandMethod("ТОРИЕНТ")]
        public static void TextOrientBy2Points()
        {
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
        public static void TextOrientByPolilineSegment()
        {
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

        private static bool SegmentCheck(Point2d point, LineSegment2d entity)
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

        private static double CalculateTextOrient(Vector2d v2d)
        {
            return (v2d.Angle * 180 / Math.PI > 270 || v2d.Angle * 180 / Math.PI < 90) ? v2d.Angle : v2d.Angle + Math.PI;
        }

        private static LineSegment2d GetPolylineSegment(Polyline polyline, Point3d point)
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

}
