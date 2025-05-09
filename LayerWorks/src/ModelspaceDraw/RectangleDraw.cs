﻿//System

//Modules
//nanoCAD
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки пустого прямоугольника
    /// </summary>
    public class RectangleDraw : AreaDraw
    {
        public RectangleDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public RectangleDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        internal double RectangleWidth => ParseRelativeValue(LegendDrawTemplate!.Width ?? "1*", LegendGrid.CellWidth);
        internal double RectangleHeight => ParseRelativeValue(LegendDrawTemplate!.Height ?? "1*", LegendGrid.CellHeight);
        /// <inheritdoc/>
        protected override void CreateEntities()
        {
            DrawRectangle(RectangleWidth, RectangleHeight);
        }

        private protected Polyline DrawRectangle(double width, double height, string? layer = null, double brightnessshift = 0d)
        {
            Polyline rectangle = new Polyline();
            rectangle.AddVertexAt(0, GetRelativePoint(-width / 2, -height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(1, GetRelativePoint(-width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(2, GetRelativePoint(width / 2, height / 2), 0, 0d, 0d);
            rectangle.AddVertexAt(3, GetRelativePoint(width / 2, -height / 2), 0, 0d, 0d);
            rectangle.Closed = true;
            string separator = NameParser.Current.Separator;
            if (layer != null)
                _checker.Check(layer);
            rectangle.Layer = layer == null ? LayerWrapper.BoundLayer.Name : layer;

            var formatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>();
            formatter?.FormatEntity(rectangle, LayerWrapper.LayerInfo.TrueName);

            rectangle.Color = BrightnessShift(brightnessshift);
            EntitiesList.Add(rectangle);
            return rectangle;
        }
    }
}
