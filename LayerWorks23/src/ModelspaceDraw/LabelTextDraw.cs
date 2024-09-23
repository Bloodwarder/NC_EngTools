//System

//Modules
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using NameClassifiers;
using NanocadUtilities;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Отрисовывает надпись
    /// </summary>
    public class LabelTextDraw : ObjectDraw
    {
        private static readonly RecordLayerWrapper? _layer;
        private readonly bool _italic = false;
        private readonly string _text;
        static LabelTextDraw()
        {
            RecordLayerWrapper? layerWrapper = null;
            EventHandler listener = (object? o, EventArgs e) => { layerWrapper = new((LayerTableRecord)o!); };
            LayerChecker.LayerAddedEvent += listener;
            LayerChecker.Check(string.Concat(LayerWrapper.StandartPrefix, "_Условные"));
            LayerChecker.LayerAddedEvent -= listener;
            _layer = layerWrapper;
        }
        internal LabelTextDraw(Point2d basepoint, string label, bool italic = false) : base(basepoint, _layer!)
        {
            _italic = italic;
            _text = label;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            string legendTextLayer = string.Concat(LayerWrapper.StandartPrefix, "_Условные");
            LayerChecker.Check(legendTextLayer);
            MText mtext = new()
            {
                Contents = _italic ? $"{{\\fArial|b0|i1|c204|p34;{_text}}}" : _text,
                TextStyleId = txtstyletable!["Standard"],
                TextHeight = LegendGrid.TextHeight,
                Layer = legendTextLayer,
                Color = s_byLayer,
                LineSpacingFactor = 0.8d
            };
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleLeft);

            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            EntitiesList.Add(mtext);
        }
    }
}
