//System
using System;
using System.Collections.Generic;


//Modules
using LayerWorks.LayerProcessing;
using LayersIO.DataTransfer;
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
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public AreaDraw() { }
        internal AreaDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer) { }
        internal AreaDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
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
        protected void DrawHatch(IEnumerable<Polyline> borders, string patternname = "SOLID", double patternscale = 0.5d, double angle = 45d, double increasebrightness = 0.8)
        {
            Hatch hatch = new Hatch();
            //ДИКИЙ БЛОК, ПЫТАЮЩИЙСЯ ОБРАБОТАТЬ ОШИБКИ ДЛЯ НЕПОНЯТНЫХ ШТРИХОВОК
            try
            {
                hatch.SetHatchPattern(!patternname.Contains("ANSI") ? HatchPatternType.PreDefined : HatchPatternType.UserDefined, patternname); // ВОЗНИКАЮТ ОШИБКИ ОТОБРАЖЕНИЯ ШТРИХОВОК "DASH" и "HONEY"
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
            hatch.HatchStyle = HatchStyle.Normal;
            foreach (Polyline pl in borders)
            {
                Point2dCollection vertexCollection = new Point2dCollection(pl.NumberOfVertices);
                DoubleCollection bulgesCollection = new DoubleCollection(pl.NumberOfVertices);
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    vertexCollection.Add(pl.GetPoint2dAt(i));
                    bulgesCollection.Add(pl.GetBulgeAt(i));
                }
                hatch.AppendLoop(HatchLoopTypes.Polyline, vertexCollection, bulgesCollection);
            }
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
            hatch.Layer = Layer.BoundLayer.Name;
            ;
            EntitiesList.Add(hatch);
        }
    }
}
