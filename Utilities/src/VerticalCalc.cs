using HostMgd.EditorInput;
using LoaderCore;
using LoaderCore.Configuration;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Globalization;
using System.Text;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using static Utilities.EntitySelector;

namespace Utilities
{
    /// <summary>
    /// Класс команд для вертикальной планировки
    /// </summary>
    public static class VerticalCalc
    {
        const string WrongEntityErrorString = "Выбран неверный объект. Завершение команды";

        static VerticalCalc()
        {
            var configSection = NcetCore.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("UtilitiesConfiguration:Vertical");
            Vertical parameters = new Vertical();
            configSection.Bind(parameters);

            BlackMarkTag = parameters.BlackMarkTag ?? "СУЩ_ОТМ";
            RedMarkTag = parameters.RedMarkTag ?? "КР_ОТМ";
            SlopeTag = parameters.SlopeTag ?? "УКЛОН";
            DistanceTag = parameters.DistanceTag ?? "РАССТОЯНИЕ";
            ElevationMarkBlockName = parameters.ElevationMarkBlockName ?? "ВП_отметки_блок_241120";
            SlopeBlockName = parameters.SlopeBlockName ?? "ВП уклоны блок_041219";

        }

        private static string BlackMarkTag { get; set; }
        private static string RedMarkTag { get; set; }
        private static string SlopeTag { get; set; }
        private static string DistanceTag { get; set; }
        private static string ElevationMarkBlockName { get; set; }
        private static string SlopeBlockName { get; set; }

        private static double LastHorStep { get; set; } = 0.2d;
        private static double LastWidthSlope { get; set; } = 20;

        public static void SlopeCalc()
        {
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


                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag), CultureInfo.InvariantCulture);
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag), CultureInfo.InvariantCulture);
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

        public static void NextMark()
        {
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



                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag), CultureInfo.InvariantCulture);
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

        public static void AverageLevel()
        {
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
                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag), CultureInfo.InvariantCulture);
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag), CultureInfo.InvariantCulture);
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

        public static void HorizontalCalc()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    // Получаем объекты чертежа для участка
                    if (!TryGetEntity("Выберите блок первой отметки", out BlockReference? mark1, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите блок второй отметки", out BlockReference? mark2, ElevationMarkBlockName))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }
                    if (!TryGetEntity("Выберите ось", out Polyline? axis))
                    { Workstation.Editor.WriteMessage(WrongEntityErrorString); return; }

                    // Получаем параметры участка
                    PromptDoubleOptions pdo = new($"Укажите шаг горизонталей ")
                    {
                        UseDefaultValue = true,
                        DefaultValue = LastHorStep
                    };
                    PromptDoubleResult result = Workstation.Editor.GetDouble(pdo);
                    if (result.Status != PromptStatus.OK)
                    {
                        Workstation.Editor.WriteMessage("Неверный ввод");
                        return;
                    }
                    PromptDoubleOptions pdo2 = new($"Укажите поперечный уклон в промилле ")
                    {
                        UseDefaultValue = true,
                        DefaultValue = LastWidthSlope
                    };
                    PromptDoubleResult result2 = Workstation.Editor.GetDouble(pdo2);
                    if (result2.Status != PromptStatus.OK)
                    {
                        Workstation.Editor.WriteMessage("Неверный ввод");
                        return;
                    }
                    double widthSlope = result2.Value/1000;
                    LastWidthSlope = result2.Value;

                    PromptDistanceOptions pDistOpts = new("Укажите полуширину проезжей части")
                    {
                        DefaultValue = 3d,
                        AllowNegative = false,
                        AllowNone = false,
                        Only2d = true,
                        UseDashedLine = true,
                    };
                    PromptDoubleResult result3 = Workstation.Editor.GetDistance(pDistOpts);
                    if (result3.Status != PromptStatus.OK)
                    {
                        Workstation.Editor.WriteMessage("Неверный ввод");
                        return;
                    }
                    double halfWidth = result3.Value;

                    double horStep = Math.Clamp(result.Value, 0.1d, 5d);
                    LastHorStep = horStep;

                    // Получить значения для расчёта
                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag), CultureInfo.InvariantCulture);
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag), CultureInfo.InvariantCulture);
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

                    var closestPoint = axis.GetClosestPointTo(mark1!.Position, false);
                    var roundedParameter = Math.Round(axis.GetParameterAtPoint(closestPoint), 0);
                    if (roundedParameter != 0)
                        axis.ReverseCurve();
                    for (double dist = axisDisplacement; dist < axis.Length; dist += axisStep)
                    {
                        var point = axis.GetPointAtDist(dist).Convert2d(new Plane());
                        double displacement = halfWidth * widthSlope / slope;
                        int segment = (int)Math.Floor(axis.GetParameterAtDistance(dist));
                        double angle = axis.GetLineSegment2dAt(segment).Direction.Angle;
                        // TODO: проверить дугу
                        var pl = CreateIsoline(point, halfWidth, displacement, angle, upwards);
                        pl.LayerId = Workstation.Database.Clayer;
                        Workstation.ModelSpace.AppendEntity(pl);
                    }


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

        private static Polyline CreateIsoline(Point2d pointOnAxis, double halfWidth, double displacement, double angle, bool isUpwardsDirected)
        {
            double sign = isUpwardsDirected ? 1 : -1;
            double x1 = pointOnAxis.X - halfWidth * Math.Sin(angle) + sign * displacement * Math.Cos(angle);
            double y1 = pointOnAxis.Y + halfWidth * Math.Cos(angle) + sign * displacement * Math.Sin(angle);
            Point2d p1 = new Point2d(x1, y1);

            double x2 = pointOnAxis.X + halfWidth * Math.Sin(angle) + sign * displacement * Math.Cos(angle);
            double y2 = pointOnAxis.Y - halfWidth * Math.Cos(angle) + sign * displacement * Math.Sin(angle);
            Point2d p2 = new Point2d(x2, y2);


            Polyline polyline = new();
            polyline.AddVertexAt(0, p1, 0, 0, 0);
            polyline.AddVertexAt(1, pointOnAxis, 0, 0, 0);
            polyline.AddVertexAt(2, p2, 0, 0, 0);

            return polyline;
        }

        public static void RedBlackEqual()
        {
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
                    double elevation = double.Parse(GetBlockAttribute((BlockReference)entity, BlackMarkTag), CultureInfo.InvariantCulture);
                    SetBlockAttribute((BlockReference)entity, RedMarkTag, elevation.ToString("0.00"));
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
                return atrref.TextString ?? "";
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

}
