//System

//Modules
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
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
        private static readonly ObjectId _layer;
        private readonly bool _italic = false;
        private readonly string _text;
        static LabelTextDraw()
        {
            _layer = LayerChecker.ForceCheck(string.Concat(LayerWrapper.StandartPrefix, "_Условные"));
        }
        public LabelTextDraw(Point2d basepoint, string label, bool italic = false) : base(basepoint, _layer!) // BREAKING BUG: слой с условными не парсится (и не должен)
        {
            _italic = italic;
            _text = label;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var txtstyletable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            MText mtext = new()
            {
                Contents = _italic ? $"{{\\fArial|b0|i1|c204|p34;{_text}}}" : _text,
                TextStyleId = txtstyletable!["Standard"],
                TextHeight = LegendGrid.TextHeight,
                LayerId = _layer,
                Color = s_byLayer,
                LineSpacingFactor = 0.8d
            };
            mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleLeft);

            mtext.Location = new Point3d(Basepoint.X, Basepoint.Y, 0d);
            EntitiesList.Add(mtext);
        }
    }
}
