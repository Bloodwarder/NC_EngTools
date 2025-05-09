﻿using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LayerWorks.ModelspaceDraw;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.Legend
{
    internal class LegendGridCell : ICloneable
    {
        private static readonly Dictionary<string, Type> _legendDrawTypes = new();
        private static readonly IRepository<string, LegendDrawTemplate> _repository;
        List<LegendObjectDraw> _draw = new();
        private readonly LegendDrawTemplate? _template;

        static LegendGridCell()
        {
            _repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendDrawTemplate>>();
        }
        internal LegendGridCell(RecordLayerWrapper layer)
        {
            Layer = layer;
            bool success = _repository.TryGet(layer.LayerInfo.TrueName, out LegendDrawTemplate? ldt);
            if (success)
                _template = ldt;
            else
                throw new Exception("Нет шаблона для отрисовки");
        }

        internal LegendGrid? ParentGrid { get; set; }
        internal LegendGridRow? ParentRow { get; set; }
        internal RecordLayerWrapper Layer { get; set; }
        internal CellTableIndex TableIndex;

        internal void AssignX(int x)
        {
            TableIndex.X = x;
        }
        internal void AssignY(int y)
        {
            TableIndex.Y = y;
        }
        public void CreateDrawObject()
        {
            bool success = _legendDrawTypes.TryGetValue(_template!.DrawTemplate!, out Type? templateType);
            if (!success)
            {
                templateType = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Name == string.Concat(_template.DrawTemplate,"Draw")).FirstOrDefault();
                if (templateType != null)
                {
                    _legendDrawTypes.Add(_template!.DrawTemplate!, templateType);
                }
                else
                {
                    throw new Exception("Тип отсутствует в сборке");
                }
            }
            double x = ParentGrid!.BasePoint.X + TableIndex.X * (LegendGrid.CellWidth + LegendGrid.WidthInterval) + LegendGrid.CellWidth / 2;
            double y = ParentGrid!.BasePoint.Y - TableIndex.Y * (LegendGrid.CellHeight + LegendGrid.HeightInterval) + LegendGrid.CellHeight / 2;
            Point2d point = new(x, y);
            if (Activator.CreateInstance(templateType!, point, Layer, _template) is not LegendObjectDraw lod)
                throw new Exception($"Отсутствует тип");
            _draw.Add(lod);
        }

        public List<Entity> Draw()
        {
            List<Entity> list = new();
            foreach (LegendObjectDraw lod in _draw)
            {
                lod.Draw();
                list.AddRange(lod.EntitiesList);
            }
            return list;
        }
        public object Clone()
        {
            LegendGridCell lgc = (LegendGridCell)MemberwiseClone();
            lgc._draw = new List<LegendObjectDraw>();
            lgc.TableIndex = new CellTableIndex();
            lgc.ParentRow = null;
            return lgc;
        }
    }
}
