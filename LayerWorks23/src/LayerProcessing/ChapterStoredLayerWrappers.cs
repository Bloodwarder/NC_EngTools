using HostMgd.ApplicationServices;
using NanocadUtilities;
using Teigha.DatabaseServices;
using LayerWorks.Commands;

namespace LayerWorks.LayerProcessing
{
    internal static class ChapterStoredLayerWrappers
    {
        private static readonly Dictionary<Document, bool> _eventAssigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<ChapterStoreLayerWrapper>> StoredLayerStates { get; } = new Dictionary<Document, List<ChapterStoreLayerWrapper>>();
        internal static void Add(ChapterStoreLayerWrapper lp)
        {
            Document doc = Workstation.Document;
            // Сохранить парсер в словарь по ключу-текущему документу
            if (!StoredLayerStates.ContainsKey(doc))
            {
                StoredLayerStates[doc] = new List<ChapterStoreLayerWrapper>();
                _eventAssigned.Add(doc, false);
            }
            if (!StoredLayerStates[doc].Any(l => l.LayerInfo.Name == lp.LayerInfo.Name))
            {
                StoredLayerStates[doc].Add(lp);
            }
        }
        // восстановление состояний слоёв при вызове командой
        internal static void Reset()
        {
            Document doc = Workstation.Document;
            foreach (ChapterStoreLayerWrapper lp in StoredLayerStates[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            _eventAssigned[doc] = false;
        }

        // восстановление состояний слоёв при вызове событием сохранения чертежа
        internal static void Reset(object sender, DatabaseIOEventArgs e)
        {
            Database db = sender as Database;
            Document doc = Application.DocumentManager.GetDocument(db);
            if (e.FileName != doc.Name)
                return;
            Teigha.DatabaseServices.TransactionManager tm = db.TransactionManager; //Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                foreach (ChapterStoreLayerWrapper lp in StoredLayerStates[doc])
                {
                    LayerTableRecord _ = (LayerTableRecord)transaction.GetObject(lp.BoundLayer.Id, OpenMode.ForWrite);
                    lp.Reset();
                }

                doc.Database.BeginSave -= Reset;
                _eventAssigned[doc] = false;
                ChapterVisualizer.ActiveChapterState[doc] = null;
                Flush(doc);
                transaction.Commit();
            }

        }
        internal static void Highlight(string primaryClassifier)
        {
            Document doc = Workstation.Document;
            if (!_eventAssigned[doc])
            {
                doc.Database.BeginSave += Reset;
                _eventAssigned[doc] = true;
            }
            foreach (ChapterStoreLayerWrapper lp in StoredLayerStates[doc]) { lp.Push(primaryClassifier, new() { "пр", "неутв" }); }
        }

        //Сбросить сохранённые состояния слоёв для текущего документа
        internal static void Flush(Document doc = null)
        {
            if (doc == null)
                doc = Workstation.Document;
            foreach (ChapterStoreLayerWrapper cslp in StoredLayerStates[doc])
                cslp.BoundLayer.Dispose();
            StoredLayerStates[doc].Clear();
        }
    }
}


