﻿//System
using Microsoft.Extensions.DependencyInjection;
//Modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки непрерывной линии
    /// </summary>
    public class SolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public SolidLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public SolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected override void CreateEntities()
        {
            Polyline pl = new Polyline();
            pl.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl.Layer = LayerWrapper.LayerInfo.Name;
            var formatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>();
            formatter?.FormatEntity(pl, LayerWrapper.LayerInfo.TrueName);
            EntitiesList.Add(pl);
        }
    }
}
