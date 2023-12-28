//System

//Modules
using LoaderCore.Utilities;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LayerWorks23.src.LayerProcessing;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Отрисовывает надпись
    /// </summary>
    public class LabelTextDraw : ObjectDraw
    {
        private readonly bool _italic = false;
        private readonly string _text;
        static LabelTextDraw()
        {
            LayerChecker.Check(string.Concat(LayerWrapper.StandartPrefix, "_Условные"));
        }
        internal LabelTextDraw() { }
        internal LabelTextDraw(Point2d basepoint, string label, bool italic = false) : base()
        {
            Basepoint = basepoint;
            _italic = italic;
            _text = label;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            TextStyleTable txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            string legendTextLayer = string.Concat(LayerWrapper.StandartPrefix, "_Условные");
            LayerChecker.Check(legendTextLayer);
            MText mtext = new MText
            {
                Contents = _italic ? $"{{\\fArial|b0|i1|c204|p34;{_text}}}" : _text,
                TextStyleId = txtstyletable["Standard"],
                TextHeight = LegendGrid.LegendGridParameters.TextHeight,
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
