//System
//Microsoft
using Microsoft.Extensions.DependencyInjection;
//Modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
//nanoCAD
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Класс для отрисовки двойной линии (одна над другой, верхняя линия по стандарту слоя)
    /// </summary>
    public class DoubleSolidLineDraw : LegendObjectDraw
    {
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public DoubleSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer) { }
        public DoubleSolidLineDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
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
            Polyline pl2 = (Polyline)pl.Clone();

            var formatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>();
            formatter?.FormatEntity(pl, LayerWrapper.LayerInfo.TrueName);
            // UNDONE : Не реализована отрисовка двойной полилинии
            pl2.ConstantWidth = double.Parse(LegendDrawTemplate?.Width ?? "1.5");  // ТОЖЕ КОСТЫЛЬ, ЧТОБЫ НЕ ДОБАВЛЯТЬ ДОП ПОЛЕ В ТАБЛИЦУ. ТАКИХ СЛОЯ ВСЕГО 3
            pl2.Color = Color.FromRgb(0, 0, 255);   // ЗАТЫЧКА, ПОКА ТАКОЙ ОБЪЕКТ ВСЕГО ОДИН
            EntitiesList.Add(pl2);
            EntitiesList.Add(pl);
        }
    }
}
