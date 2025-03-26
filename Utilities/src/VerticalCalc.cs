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
            Vertical parameters = new();
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
        private static double LastHalfWidth { get; set; } = 3.0d;

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

        public static void IsolinesCalc()
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
                    double widthSlope = result2.Value / 1000d;
                    LastWidthSlope = result2.Value;

                    PromptDistanceOptions pDistOpts = new("Укажите полуширину проезжей части")
                    {
                        DefaultValue = LastHalfWidth,
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
                    LastHalfWidth = halfWidth;

                    double horStep = Math.Clamp(result.Value, 0.1d, 5d);
                    LastHorStep = horStep;

                    // Получить значения для расчёта из объектов чертежа
                    double red1 = double.Parse(GetBlockAttribute(mark1!, RedMarkTag), CultureInfo.InvariantCulture);
                    double red2 = double.Parse(GetBlockAttribute(mark2!, RedMarkTag), CultureInfo.InvariantCulture);
                    double l1 = axis!.Length;

                    bool upwards = red2 > red1;
                    double slope = Math.Abs((red2 - red1) / l1);
                    double axisStep = horStep / slope;

                    double scaleDifference = Math.Round(red1 % 1d, 2) * 100d;
                    double horStep100 = horStep * 100d;
                    scaleDifference %= horStep100;

                    double levelDisplacementInt = upwards ? horStep100 - scaleDifference : scaleDifference;
                    double levelDisplacement = levelDisplacementInt / 100d;
                    double axisDisplacement = levelDisplacement / slope;

                    StringBuilder sb = new();
                    sb.Append($"\nУклон: {slope * 1000d:0}");
                    sb.Append($"\nШаг на оси: {axisStep:0.0}");
                    sb.Append($"\nСмещение на оси от первой отметки: {axisDisplacement:0.0}");
                    string textContent = sb.ToString();

                    Workstation.Editor.WriteMessage(textContent);

                    BlockTableRecord modelSpace = Workstation.ModelSpace;

                    var closestPoint = axis.GetClosestPointTo(mark1!.Position, false);
                    var roundedParameter = Math.Round(axis.GetParameterAtPoint(closestPoint), 0);
                    if (roundedParameter != 0)
                    {
                        //upwards = !upwards;
                        axis.ReverseCurve();
                    }

                    double currentHeight = Math.Round(red1 + (upwards ? 1d : -1d) * levelDisplacement, 2);
                    for (double dist = axisDisplacement; dist < axis.Length; dist += axisStep)
                    {
                        var point = axis.GetPointAtDist(dist).Convert2d(new Plane());
                        double displacement = halfWidth * widthSlope / slope;
                        int segmentNumber = (int)Math.Floor(axis.GetParameterAtDistance(dist));
                        double angle = axis.GetLineSegment2dAt(segmentNumber).Direction.Angle;
                        // TODO: проверить дугу
                        var pl = CreateIsoline(point, halfWidth, displacement, angle, upwards);
                        pl.LayerId = Workstation.Database.Clayer;
                        Entity[] dashAndLabel = CreateLabelAndDash(pl, currentHeight, upwards);
                        currentHeight = upwards ? currentHeight + horStep : currentHeight - horStep;
                        modelSpace.AppendEntity(pl);
                        modelSpace.AppendEntity(dashAndLabel[0]);
                        modelSpace.AppendEntity(dashAndLabel[1]);
                        transaction.AddNewlyCreatedDBObject(pl, true);
                        transaction.AddNewlyCreatedDBObject(dashAndLabel[0], true);
                        transaction.AddNewlyCreatedDBObject(dashAndLabel[1], true);
                    }

                    double vx = mark1!.Position.X - mark2!.Position.X;
                    double vy = mark1.Position.Y - mark2.Position.Y;
                    MText mText = new()
                    {
                        BackgroundFill = false,
                        Attachment = vx > 0 ? AttachmentPoint.TopRight : AttachmentPoint.TopLeft,
                        Color = Color.FromRgb(0, 0, 255),
                        TextHeight = 2d,
                        Location = mark1.Position,
                        Rotation = Math.Atan(vy / vx),
                        Contents = textContent,
                        LayerId = Workstation.Database.Clayer
                    };
                    modelSpace.AppendEntity(mText);
                    transaction.AddNewlyCreatedDBObject(mText, true);
                }
                finally
                {
                    Highlighter.Unhighlight();
                }
                transaction.Commit();
            }
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
        private static Polyline CreateIsoline(Point2d pointOnAxis, double halfWidth, double displacement, double angle, bool isUpwardsDirected)
        {
            double sign = isUpwardsDirected ? 1d : -1d;
            double x1 = pointOnAxis.X - halfWidth * Math.Sin(angle) + sign * displacement * Math.Cos(angle);
            double y1 = pointOnAxis.Y + halfWidth * Math.Cos(angle) + sign * displacement * Math.Sin(angle);
            Point2d p1 = new(x1, y1);

            double x2 = pointOnAxis.X + halfWidth * Math.Sin(angle) + sign * displacement * Math.Cos(angle);
            double y2 = pointOnAxis.Y - halfWidth * Math.Cos(angle) + sign * displacement * Math.Sin(angle);
            Point2d p2 = new(x2, y2);


            Polyline polyline = new();
            polyline.AddVertexAt(0, p1, 0, 0, 0);
            polyline.AddVertexAt(1, pointOnAxis, 0, 0, 0);
            polyline.AddVertexAt(2, p2, 0, 0, 0);

            return polyline;
        }

        private static Entity[] CreateLabelAndDash(Polyline polyline, double labelValue, bool upwards)
        {
            labelValue = Math.Round(labelValue, 2);
            Point3d point = polyline.GetPointAtParameter(1.6d);
            Vector3d direction = polyline.GetLineSegmentAt(1).Direction;
            double angle = direction.Convert2d(new Plane()).Angle;
            double labelOffset = Labeler.LabelTextHeight * 0.1d;

            double m = upwards ? -1d : 1d;

            double xLabel = point.X + m * Math.Sin(angle) * labelOffset;
            double yLabel = point.Y - m * Math.Cos(angle) * labelOffset;
            double xDash = point.X - m * Math.Sin(angle) * 1d;
            double yDash = point.Y + m * Math.Cos(angle) * 1d;

            Point2d dashPoint = new(xDash, yDash);
            Point3d labelPoint = new(xLabel, yLabel, 0d);

            bool boldIsoline = labelValue % 1d == 0d;

            MText label = new()
            {
                Attachment = AttachmentPoint.BottomCenter,
                Rotation = angle + (upwards ? 0d : 1d) * Math.PI,
                LayerId = Workstation.Database.Clayer,
                Contents = boldIsoline ? labelValue.ToString("0.00", CultureInfo.InvariantCulture) : Math.Round(labelValue % 1d * 100d, 0).ToString("0", CultureInfo.InvariantCulture),
                Location = labelPoint,
                TextHeight = Labeler.LabelTextHeight * 0.9d
            };


            Polyline dash = new()
            {
                LayerId = Workstation.Database.Clayer,
            };
            dash.AddVertexAt(0, point.Convert2d(new Plane()), 0, 0, 0);
            dash.AddVertexAt(1, dashPoint, 0, 0, 0);

            return new Entity[] { label, dash };
        }
        private static string GetBlockAttribute(BlockReference bref, string tag)
        {
            AttributeCollection atrs = bref.AttributeCollection;
            var atrref = atrs.Cast<ObjectId>()
                             .Select(id => id.GetObject<AttributeReference>(OpenMode.ForRead))
                             .FirstOrDefault(r => r.Tag == tag);
            //var atrref = (from ObjectId objid in atrs
            //              let rfr = Workstation.TransactionManager.TopTransaction.GetObject(objid, OpenMode.ForRead) as AttributeReference
            //              where rfr.Tag == tag
            //              select rfr).FirstOrDefault();
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
