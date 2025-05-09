﻿//System
//Microsoft
using Microsoft.Extensions.DependencyInjection;

//Modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LoaderCore;
using LoaderCore.Interfaces;
//nanoCAD
using Teigha.Colors;
using Teigha.Geometry;
using Teigha.DatabaseServices;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки элемента легенды
    /// </summary>
    public abstract class LegendObjectDraw : ObjectDraw
    {

        private static readonly IRepository<string, LegendDrawTemplate> _repository;
        private LegendDrawTemplate? _legendDrawTemplate;
        
        static LegendObjectDraw()
        {
            _repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendDrawTemplate>>();
        }

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
        public LegendObjectDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer)
        {
            LayerWrapper = layer;
            bool success = _repository.TryGet(LayerWrapper.LayerInfo.TrueName, out var legendDrawTemplate);
            if (success)
                LegendDrawTemplate = legendDrawTemplate;
        }
        public LegendObjectDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : this(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal static double CellWidth => LegendGrid.CellWidth;
        internal static double CellHeight => LegendGrid.CellHeight;

        internal RecordLayerWrapper LayerWrapper { get; set; }

        public sealed override void Draw()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало отрисовки объекта слоя {LayerName}", this.GetType().Name, LayerWrapper.BoundLayer.Name);
            try
            {
                CreateEntities();
                Workstation.Logger?.LogDebug("{ProcessingObject}: Успешная отрисовка объекта слоя {LayerName}. Создано {Count} объектов",
                                             this.GetType().Name,
                                             LayerWrapper.BoundLayer.Name,
                                             EntitiesList.Count);
            }
            catch (Exception ex)
            {
                Workstation.Logger?.LogWarning(ex,
                                               "{ProcessingObject}: Не удалась отрисовка объекта слоя {LayerName}. Ошибка - {Exception}",
                                               this.GetType().Name,
                                               LayerWrapper.BoundLayer.Name,
                                               ex.Message);
            }
        }

        protected abstract void CreateEntities(); 

        private protected static double ParseRelativeValue(string value, double absolute)
        {
            return value.EndsWith("*") ? double.Parse(value.Replace("*", "")) * absolute : double.Parse(value);
        }

        private protected Color BrightnessShift(double value)
        {
            if (value == 0)
                return s_byLayer;
            Color color = LayerWrapper.BoundLayer.Color;
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
