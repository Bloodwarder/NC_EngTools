﻿//System

//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;

//internal modules
using NanocadUtilities;
using NameClassifiers;
using LayerWorks.LayerProcessing;
using LayerWorks.LayerProcessing;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс для визуализации объектов по разделам на основе данных в LayerParser
    /// </summary>
    public class ChapterVisualizer
    {
        private static readonly Dictionary<Document, string> _activeChapterState = new Dictionary<Document, string>();

        internal static Dictionary<Document, string> ActiveChapterState
        {
            get
            {
                Document doc = Workstation.Document;
                if (!_activeChapterState.ContainsKey(doc))
                { _activeChapterState.Add(doc, null); }
                return _activeChapterState;
            }
        }

        /// <summary>
        /// Подсветить слои для выбранного раздела (выключить остальные и визуализировать переустройство)
        /// </summary>
        [CommandMethod("ВИЗРАЗДЕЛ")]
        public void Visualizer()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;
            Document doc = Workstation.Document;
            Database db = Workstation.Database;

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = (LayerTable)transaction.GetObject(db.LayerTableId, OpenMode.ForRead, false);
                var layers = from ObjectId elem in lt
                             let ltr = tm.GetObject(elem, OpenMode.ForWrite, false) as LayerTableRecord
                             where ltr.Name.StartsWith(LayerWrapper.StandartPrefix + NameParser.LoadedParsers[LayerWrapper.StandartPrefix].Separator)
                             select ltr;
                int errorCount = 0;
                int successCount = 0;
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        new ChapterStoreLayerWrapper(ltr);
                        successCount++;
                    }
                    catch (WrongLayerException)
                    {
                        //editor.WriteMessage(ex.Message);
                        errorCount++;
                        continue;
                    }
                }
                Workstation.Editor.WriteMessage($"Фильтр включен для {successCount} слоёв. Число необработанных слоёв: {errorCount}");

                var layerchapters = ChapterStoredLayerWrappers.StoredLayerStates[doc]
                    .Where(l => l.LayerInfo.PrimaryClassifier != null)
                    .Select(l => l.LayerInfo.PrimaryClassifier)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
                List<string> lcplus = layerchapters.Append("Сброс").ToList();
                PromptKeywordOptions pko = new PromptKeywordOptions($"Выберите раздел [" + string.Join("/", lcplus) + "]", string.Join(" ", lcplus))
                {
                    AppendKeywordsToMessage = true,
                    AllowNone = false,
                    AllowArbitraryInput = false
                };
                PromptResult result = editor.GetKeywords(pko);
                if (result.Status != PromptStatus.OK) { return; }
                if (result.StringResult == "Сброс")
                {
                    ChapterStoredLayerWrappers.Reset();
                    if (ActiveChapterState != null)
                    {
                        LayerChecker.LayerAddedEvent -= NewLayerHighlight;
                        ActiveChapterState[doc] = null;
                    }

                }
                else
                {
                    ActiveChapterState[doc] = result.StringResult;
                    ChapterStoredLayerWrappers.Highlight(ActiveChapterState[doc]);
                    LayerChecker.LayerAddedEvent += NewLayerHighlight;
                }
                transaction.Commit();
            }
        }

        internal void NewLayerHighlight(object sender, EventArgs e)
        {
            Document doc = Workstation.Document;
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                ChapterStoreLayerWrapper cslp = new ChapterStoreLayerWrapper((LayerTableRecord)sender);
                cslp.Push(ActiveChapterState[doc], new() { "пр", "неутв" });
                transaction.Commit();
            }

        }
    }

}






