using LoaderCore.Utilities;
using Teigha.DatabaseServices;
using LayerWorks.Commands;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с текущим слоем
    /// </summary>
    public class CurrentLayerParser : LayerParser
    {
        /// <summary>
        /// Конструктор без параметров, автоматически передающий в базовый конструктор имя текущего слоя
        /// </summary>
        public CurrentLayerParser() : base(Clayername()) { ActiveLayerParsers.Add(this); }

        private static string Clayername()
        {
            Database db = Workstation.Database;
            LayerTableRecord ltr = db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
            return ltr.Name;
        }

        /// <summary>
        /// Задаёт стандартные свойства для черчения новых объектов чертежа
        /// </summary>
        public override void Push()
        {
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;

            LayerChecker.Check(this);
            LayerTable lt = tm.TopTransaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            db.Clayer = lt[OutputLayerName];
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out _);
            db.Celtscale = lp.LTScale;
            db.Plinewid = lp.ConstantWidth;
        }
    }
}


