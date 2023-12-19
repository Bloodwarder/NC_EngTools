using HostMgd.ApplicationServices;
using LoaderCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using LayerWorks.Commands;

namespace LayerWorks.LayerProcessing
{
    internal static class ChapterStoredLayerParsers
    {
        private static readonly Dictionary<Document, bool> _eventAssigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<ChapterStoreLayerParser>> StoredLayerStates { get; } = new Dictionary<Document, List<ChapterStoreLayerParser>>();
        internal static void Add(ChapterStoreLayerParser lp)
        {
            Document doc = Workstation.Document;
            // Сохранить парсер в словарь по ключу-текущему документу
            if (!StoredLayerStates.ContainsKey(doc))
            {
                StoredLayerStates[doc] = new List<ChapterStoreLayerParser>();
                _eventAssigned.Add(doc, false);
            }
            if (!StoredLayerStates[doc].Any(l => l.InputLayerName == lp.InputLayerName))
            {
                StoredLayerStates[doc].Add(lp);
            }
        }
        // восстановление состояний слоёв при вызове командой
        internal static void Reset()
        {
            Document doc = Workstation.Document;
            foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            _eventAssigned[doc] = false;
        }

        // восстановление состояний слоёв при вызове событием сохранения чертежа
        internal static void Reset(object sender, EventArgs e)
        {
            Database db = sender as Database;
            Document doc = Application.DocumentManager.GetDocument(db);
            Teigha.DatabaseServices.TransactionManager tm = db.TransactionManager; //Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc])
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
        internal static void Highlight(string engtype)
        {
            Document doc = Workstation.Document;
            if (!_eventAssigned[doc])
            {
                doc.Database.BeginSave += Reset;
                _eventAssigned[doc] = true;
            }
            foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc]) { lp.Push(engtype); }
        }

        //Сбросить сохранённые состояния слоёв для текущего документа
        internal static void Flush(Document doc = null)
        {
            if (doc == null)
                doc = Workstation.Document;
            foreach (ChapterStoreLayerParser cslp in StoredLayerStates[doc])
                cslp.BoundLayer.Dispose();
            StoredLayerStates[doc].Clear();
        }
    }
}


