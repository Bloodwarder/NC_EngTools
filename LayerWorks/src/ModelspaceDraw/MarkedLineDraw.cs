﻿//System
using LayersIO.DataTransfer;

//Modules
using LayerWorks.LayerProcessing;
using LoaderCore.NanocadUtilities;
using LayerWorks.Legend;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки линии с вставленными символами (буквами)
    /// </summary>
    public abstract class MarkedLineDraw : LegendObjectDraw
    {
        const double RelativeWidthLimit = 0.65d;
        internal string MarkChar => LegendDrawTemplate?.MarkChar ?? "ОШБК";
        private double _width;

        public MarkedLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer)
        {
        }
        public MarkedLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }
        /// <inheritdoc/>
        protected override void CreateEntities()
        {
            DrawText();
            List<Polyline> polylines = DrawLines();
            FormatLines(polylines);
        }

        /// <summary>
        /// Создание текста для линии и его добавление в список объектов для отрисовки
        /// </summary>
        protected void DrawText()
        {
            TextStyleTable? txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            MText mtext = new()
            {
                Contents = MarkChar,
                TextStyleId = txtstyletable!["Standard"],
                TextHeight = LegendGrid.MarkedLineTextHeight,
                Layer = LayerWrapper.BoundLayer.Name,
                Color = s_byLayer,
            };

            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            _width = mtext.ActualWidth;
            if (mtext.ActualWidth > CellWidth * RelativeWidthLimit)
            {
                mtext.Contents = $"{{\\W0.8;\\T0.9;{MarkChar}}}";
                _width = mtext.ActualWidth;
            }
            EntitiesList.Add(mtext);
        }
        /// <summary>
        /// Создание полилиний и добавление их в список объектов для отрисовки
        /// </summary>
        /// <returns> Полилинии, добавленные в список объектов для отрисовки</returns>
        protected List<Polyline> DrawLines()
        {
            Polyline pl1 = new Polyline();
            Polyline pl2 = new Polyline();
            pl1.AddVertexAt(0, GetRelativePoint(-CellWidth / 2, 0d), 0, 0d, 0d);
            pl1.AddVertexAt(1, GetRelativePoint(-(_width / 2 + 0.5d), 0d), 0, 0d, 0d);
            pl1.Layer = LayerWrapper.BoundLayer.Name;

            pl2.AddVertexAt(0, GetRelativePoint(_width / 2 + 0.5d, 0d), 0, 0d, 0d);
            pl2.AddVertexAt(1, GetRelativePoint(CellWidth / 2, 0d), 0, 0d, 0d);
            pl2.Layer = LayerWrapper.BoundLayer.Name;

            List<Polyline> list = new List<Polyline> { pl1, pl2 };
            EntitiesList.AddRange(list);
            return list;
        }
        /// <summary>
        /// Придание нужных свойств линиям, созданным для данного объекта отрисовки
        /// </summary>
        /// <param name="lines">Коллекция полилиний для обработки</param>
        protected abstract void FormatLines(IEnumerable<Polyline> lines);

    }
}
