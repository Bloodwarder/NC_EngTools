using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using NameClassifiers;
using NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с текущим слоем
    /// </summary>
    public class CurrentLayerWrapper : LayerWrapper
    {
        /// <summary>
        /// Конструктор без параметров, автоматически передающий в базовый конструктор имя текущего слоя
        /// </summary>
        public CurrentLayerWrapper() : base(Clayername()) { ActiveLayerWrappers.Add(this); }

        private static string Clayername()
        {
            Database db = Workstation.Database;
            LayerTableRecord? ltr = db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
            return ltr!.Name;
        }

        /// <summary>
        /// Задаёт стандартные свойства для черчения новых объектов чертежа
        /// </summary>
        public override void Push()
        {
            TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;

            LayerChecker.Check(this);
            LayerTable? lt = tm.TopTransaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            db.Clayer = lt![LayerInfo.Name];
            bool success = LayerPropertiesDictionary.TryGetValue(LayerInfo.Name, out LayerProps? lp);
            if (success)
            {
                db.Celtscale = lp?.LTScale ?? default;
                db.Plinewid = lp?.ConstantWidth ?? default;
            }
        }
    }
}


