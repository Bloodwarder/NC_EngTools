using HostMgd.ApplicationServices;
using LayerWorks.Commands;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    internal static class VisualizerLayerWrappers
    {
        private static readonly Dictionary<Document, bool> _eventAssigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<VisualizerLayerWrapper>> StoredLayerStates { get; } = new Dictionary<Document, List<VisualizerLayerWrapper>>();
        internal static void Add(VisualizerLayerWrapper lp)
        {
            Document doc = Workstation.Document;
            // Сохранить парсер в словарь по ключу-текущему документу
            if (!StoredLayerStates.ContainsKey(doc))
            {
                StoredLayerStates[doc] = new List<VisualizerLayerWrapper>();
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
            foreach (VisualizerLayerWrapper lp in StoredLayerStates[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            _eventAssigned[doc] = false;
        }

        // восстановление состояний слоёв при вызове событием сохранения чертежа
        internal static void Reset(object? sender, DatabaseIOEventArgs e)
        {
            Database db = (Database)sender!;
            Document doc = Application.DocumentManager.GetDocument(db);
            if (e.FileName != doc.Name)
                return;
            Teigha.DatabaseServices.TransactionManager tm = db.TransactionManager; //Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                foreach (VisualizerLayerWrapper lp in StoredLayerStates[doc])
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
        internal static void Highlight(string? primaryClassifier)
        {
            Document doc = Workstation.Document;
            if (!_eventAssigned[doc])
            {
                doc.Database.BeginSave += Reset;
                _eventAssigned[doc] = true;
            }
            foreach (VisualizerLayerWrapper lw in StoredLayerStates[doc])
            {
                lw.Push(primaryClassifier, new() { "пр", "неутв" });
            }
        }

        //Сбросить сохранённые состояния слоёв для текущего документа
        internal static void Flush(Document? doc = null)
        {
            if (doc == null)
                doc = Workstation.Document;
            foreach (VisualizerLayerWrapper cslp in StoredLayerStates[doc])
                cslp.BoundLayer.Dispose();
            StoredLayerStates[doc].Clear();
        }
    }
}


