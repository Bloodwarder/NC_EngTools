//System


//Modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Абстрактный класс для отрисовки заштрихованных объектов
    /// </summary>
    public abstract class AreaDraw : LegendObjectDraw
    {
        protected const string DefaultHatchPatternName = "SOLID";

        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public AreaDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public AreaDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        /// <summary>
        /// Отрисовка штриховки
        /// </summary>
        /// <param name="borders"> Объекты контура </param>
        /// <param name="patternname"> Имя образца </param>
        /// <param name="patternscale"> Масштаб образца </param>
        /// <param name="angle"> Угол поворота </param>
        /// <param name="increasebrightness"> Изменение яркости относительно базового цвета слоя </param>
        protected void DrawHatch(IEnumerable<Polyline> borders, string patternname = DefaultHatchPatternName, double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8)
        {
            Hatch hatch = new()
            {
                Layer = LayerWrapper.BoundLayer.Name
            };
            foreach (Polyline pl in borders)
            {
                Point2dCollection vertexCollection = new(pl.NumberOfVertices);
                DoubleCollection bulgesCollection = new(pl.NumberOfVertices);
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    vertexCollection.Add(pl.GetPoint2dAt(i));
                    bulgesCollection.Add(pl.GetBulgeAt(i));
                }
                hatch.AppendLoop(HatchLoopTypes.Polyline, vertexCollection, bulgesCollection);
            }
            hatch.HatchStyle = HatchStyle.Normal;
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
            //ДИКИЙ БЛОК, ПЫТАЮЩИЙСЯ ОБРАБОТАТЬ ОШИБКИ ДЛЯ НЕПОНЯТНЫХ ШТРИХОВОК
            try
            {
                //hatch.SetHatchPattern(!patternname.Contains("ANSI") ? HatchPatternType.PreDefined : HatchPatternType.UserDefined, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
                hatch.SetHatchPattern(HatchPatternType.PreDefined, patternname);
            }
            catch
            {

                for (int i = 2; i > -1; i--)
                {
                    try
                    {
                        hatch.SetHatchPattern((HatchPatternType)i, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            EntitiesList.Add(hatch);
        }
    }
}
