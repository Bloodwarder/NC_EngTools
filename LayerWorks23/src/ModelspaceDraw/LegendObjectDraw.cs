﻿//System

//Modules
//nanoCAD
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Teigha.Colors;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки элемента легенды
    /// </summary>
    public abstract class LegendObjectDraw : ObjectDraw
    {
        private LegendDrawTemplate? _legendDrawTemplate;
        /// <summary>
        /// Структура с данными для отрисовки объекта
        /// </summary>
        public LegendDrawTemplate? LegendDrawTemplate
        {
            get => _legendDrawTemplate;
            set
            {
                _legendDrawTemplate = value;
                TemplateSetEventHandler?.Invoke(this, new EventArgs());
            }
        }
        /// <summary>
        /// Событие назначения объекту конкретного шаблона отрисовки.
        /// Необходимо для объектов, которые нельзя обрабатывать одномоментно и нужно поставить в очередь на обработку (например блоки, импортируемые из внешних чертежей).
        /// </summary>
        protected event EventHandler? TemplateSetEventHandler;

        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        internal LegendObjectDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer)
        {
            var service = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendDrawTemplate>>();
            bool success = service.TryGet(Layer.LayerInfo.TrueName, out var legendDrawTemplate);
            if (success)
                LegendDrawTemplate = legendDrawTemplate;
        }
        internal LegendObjectDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal static double CellWidth => LegendGrid.CellWidth;
        internal static double CellHeight => LegendGrid.CellHeight;

        private protected static double ParseRelativeValue(string value, double absolute)
        {
            return value.EndsWith("*") ? double.Parse(value.Replace("*", "")) * absolute : double.Parse(value);
        }

        private protected Color BrightnessShift(double value)
        {
            if (value == 0)
                return s_byLayer;
            Color color = Layer.BoundLayer.Color;
            if (value > 0)
            {
                color = Color.FromRgb((byte)(color.Red + (255 - color.Red) * value), (byte)(color.Green + (255 - color.Green) * value), (byte)(color.Blue + (255 - color.Blue) * value));
            }
            else if (value < 0)
            {
                color = Color.FromRgb((byte)(color.Red + color.Red * value), (byte)(color.Green + color.Green * value), (byte)(color.Blue + color.Blue * value));
            }
            return color;
        }
    }
}
