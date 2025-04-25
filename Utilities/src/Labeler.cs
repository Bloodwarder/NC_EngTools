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
using Microsoft.Extensions.Logging;

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

        internal static double LabelTextHeight { get; set; }
        private static double LabelBackgroundScaleFactor { get; set; }
        private static string PrevText { get; set; } = "";
        /// <summary>
        /// Создать подпись для сегмента полилинии
        /// </summary>
        public static void LabelDraw()
        {
            try
            {
                using (Transaction tr = Workstation.TransactionManager.StartTransaction())
                {
                    //выбираем полилинию
                    PromptEntityOptions promptEntityOptions = new("Выберите полилинию")
                    { };
                    PromptEntityOptions peo = promptEntityOptions;
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                    if (result.Status != PromptStatus.OK)
                    {
                        Workstation.Logger?.LogWarning("Ошибка выбора");
                        return;
                    }

                    //выбираем точку вставки подписи и находим ближайшую точку на полилинии
                    PromptPointOptions ppo = new("Укажите точку вставки подписи рядом с сегментом полилинии");
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
                    PromptResult pr;
                    bool isKeyword;
                    do
                    {
                        pr = Workstation.Editor.GetString(pso);
                        isKeyword = false;
                        if (pr.Status == PromptStatus.OK)
                        {
                            switch (pr.StringResult)
                            {
                                case "Высота":
                                    AssignNewTextHeight();
                                    isKeyword = true;
                                    break;
                                case "Фон":
                                    AssignBackgroundFactor();
                                    isKeyword = true;
                                    break;
                            }
                        }
                        else
                        {
                            Workstation.Logger?.LogWarning("Неверный ввод");
                            return;
                        }
                    } while (isKeyword);
                    string text = pr.StringResult;
                    PrevText = text;

                    //ищем таблицу блоков и моделспейс
                    var modelSpace = Workstation.ModelSpace;
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
                    modelSpace!.AppendEntity(mtext);
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
                Workstation.Logger?.LogWarning("Неверное значение");
            double newValue = Math.Clamp(result.Value, 1d, 2d);
            LabelBackgroundScaleFactor = newValue;
            // TODO: обновить в файле конфигурации
        }

        private static void AssignNewTextHeight()
        {
            PromptDoubleResult result = Workstation.Editor.GetDouble("Введите новое значение высоты текста:");
            if (result.Status != PromptStatus.OK)
                Workstation.Logger?.LogWarning("Неверное значение");
            double newValue = Math.Max(result.Value, 0.01d);
            LabelTextHeight = newValue;
            // TODO: обновить в файле конфигурации
        }

        public static void TextOrientBy2Points()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetTextEntity("Выберите МТекст", out Entity? textEntity))
                    { Workstation.Editor.WriteMessage("Ошибка выбора"); return; }

                    //выбираем точку вставки подписи и находим ближайшую точку на полилинии
                    PromptPointOptions ppo = new("Укажите первую точку")
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
                    if (textEntity is MText mText)
                        mText!.Rotation = angleresult.Value;
                    else if (textEntity is DBText text)
                        text.Rotation = angleresult.Value;
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }
        }

        public static void TextOrientByPolylineSegment()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!TryGetTextEntity("Выберите МТекст", out Entity? textEntity))
                    {
                        Workstation.Logger?.LogWarning("Ошибка выбора");
                        return;
                    }

                    PromptEntityOptions peo = new("Выберите полилинию")
                    { };
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                    if (result.Status != PromptStatus.OK)
                    {
                        Workstation.Logger?.LogWarning("Ошибка выбора");
                        return;
                    }

                    Polyline polyline = result.ObjectId.GetObject<Polyline>(OpenMode.ForRead, transaction);
                    LineSegment2d segment = GetPolylineSegment(polyline, result.PickedPoint);
                    double rotation = CalculateTextOrient(segment.Direction);
                    if (textEntity is MText mText)
                        mText!.Rotation = rotation;
                    else if (textEntity is DBText text)
                        text.Rotation = rotation;
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }

        }

        public static void ReplaceMTextWithMLeader()
        {
            PromptEntityOptions peo = new("Выберите Текст или МТекст")
            {
                AllowNone = false,
            };
            peo.AddAllowedClass(typeof(MText), true);
            peo.AddAllowedClass(typeof(DBText), true);
            PromptPointOptions ppo = new("Выберите местоположение выноски");
            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptEntityResult entityResult = Workstation.Editor.GetEntity(peo);
                PromptPointResult pointResult = Workstation.Editor.GetPoint(ppo);
                if (entityResult.Status != PromptStatus.OK || pointResult.Status != PromptStatus.OK)
                {
                    Workstation.Logger?.LogWarning("Ошибка выбора");
                    return;
                }
                Entity entity = entityResult.ObjectId.GetObject<Entity>(OpenMode.ForWrite, transaction);
                Point3d point = pointResult.Value;
                MText newMtext;
                MLeader mLeader = new();
                Point3d textLocation;
                if (entity is MText mText)
                {
                    newMtext = (MText)mText.Clone();
                    textLocation = mText.Location;
                }
                else if (entity is DBText text)
                {
                    newMtext = new();
                    newMtext.SetDatabaseDefaults();
                    newMtext.Contents = text.TextString;
                    newMtext.Height = text.Height;
                    newMtext.TextHeight = text.Height;
                    textLocation = text.Position;
                    newMtext.Location = textLocation;
                }
                else
                {
                    throw new InvalidOperationException("Ошибка. Неверный тип объекта");
                }
                mLeader.ContentType = ContentType.MTextContent;
                mLeader.SetDatabaseDefaults();
                mLeader.MText = newMtext;
                mLeader.TextLocation = point;
                mLeader.LayerId = entity.LayerId;

                mLeader.AddLeaderLine(textLocation);

                Workstation.ModelSpace.AppendEntity(mLeader);
                transaction.AddNewlyCreatedDBObject(mLeader, true);
                entity.Erase();
                transaction.Commit();
            }
        }

        [Obsolete]
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
            return (point.X > minx & point.X < maxx) & (point.Y > miny & point.Y < maxy);
        }

        private static double CalculateTextOrient(Vector2d v2d)
        {
            return (v2d.Angle * 180 / Math.PI > 270 || v2d.Angle * 180 / Math.PI < 90) ? v2d.Angle : v2d.Angle + Math.PI;
        }

        private static LineSegment2d GetPolylineSegment(Polyline polyline, Point3d point)
        {
            Point3d pointOnLine = polyline.GetClosestPointTo(point, false);
            int segmentNumber = (int)Math.Floor(polyline.GetParameterAtPoint(pointOnLine));
            LineSegment2d segment = polyline.GetLineSegment2dAt(segmentNumber);
            return segment;
        }
    }

}
