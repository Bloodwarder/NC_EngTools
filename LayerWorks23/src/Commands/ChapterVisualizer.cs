//System

//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using LayerWorks.HighlightFiltering;
using LayerWorks.LayerProcessing;
using NameClassifiers;
using NameClassifiers.Highlighting;
//internal modules
using NanocadUtilities;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using static NanocadUtilities.EditorHelper;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс для визуализации объектов по разделам на основе данных в LayerParser
    /// </summary>
    public class ChapterVisualizer
    {
        private static readonly Dictionary<Document, string?> _activeChapterState = new();

        internal static Dictionary<Document, string?> ActiveChapterState
        {
            get
            {
                Document doc = Workstation.Document;
                if (!_activeChapterState.ContainsKey(doc))
                    _activeChapterState.Add(doc, null);
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

            string? prefix = LayerWrapper.StandartPrefix;
            if (prefix == null)
            {
                editor.WriteMessage("Не задан префикс слоёв для выполнения команды");
                return;
            }

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = (LayerTable)transaction.GetObject(db.LayerTableId, OpenMode.ForRead, false);
                var layers = from ObjectId elem in lt
                             let ltr = tm.GetObject(elem, OpenMode.ForWrite, false) as LayerTableRecord
                             where ltr.Name.StartsWith(LayerWrapper.StandartPrefix + NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].Separator)
                             select ltr;
                int errorCount = 0;
                int successCount = 0;
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        VisualizerLayerWrapper.Create(ltr);
                        successCount++;
                    }
                    catch (WrongLayerException)
                    {
                        errorCount++;
                        continue;
                    }
                }
                Workstation.Editor.WriteMessage($"Фильтр включен для {successCount} слоёв. Число необработанных слоёв: {errorCount}");

                Visualizers visualizers = NameParser.LoadedParsers[prefix].Visualizers;
                string[] filters = visualizers.Filters.Select(x => x.Name).ToArray();
                string filterName = GetStringKeywordResult(filters, "Выберите фильтр");
                HighlightFilter chosenFilter = visualizers.Filters.Where(f => f.Name == filterName).Single();

                //chosenFilter.ApplyFilter(VisualizerLayerWrappers.StoredLayerStates[Workstation.Document]); ПЕРЕСОБРАТЬ С ЭТИМ И ТЕСТИРОВАТЬ

                // UNDONE: Работает по-старому! Собрано просто, чтобы было не сломано. Взять логику из визуализаторов
                var layerchapters = VisualizerLayerWrappers.StoredLayerStates[doc]
                                                              .Where(l => l.LayerInfo.PrimaryClassifier != null)
                                                              .Select(l => l.LayerInfo.PrimaryClassifier)
                                                              .Distinct()
                                                              .OrderBy(l => l)
                                                              .ToList();
                layerchapters.Add("Сброс");
                PromptKeywordOptions pko = new($"Выберите раздел [" + string.Join("/", layerchapters) + "]", string.Join(" ", layerchapters))
                {
                    AppendKeywordsToMessage = true,
                    AllowNone = false,
                    AllowArbitraryInput = false
                };
                PromptResult result = editor.GetKeywords(pko);
                if (result.Status != PromptStatus.OK)
                    return;
                ApplyVisualizer(doc, result.StringResult);
                transaction.Commit();
            }
        }


        internal void NewLayerHighlight(object? sender, EventArgs e)
        {
            Document doc = Workstation.Document;

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                VisualizerLayerWrapper cslp = new((LayerTableRecord)sender!);
                cslp.Push(ActiveChapterState[doc], new() { "пр", "неутв" });
                transaction.Commit();
            }

        }
        private void ApplyVisualizer(Document doc, string result)
        {
            if (result == "Сброс")
            {
                VisualizerLayerWrappers.Reset();
                if (ActiveChapterState != null)
                {
                    LayerChecker.LayerAddedEvent -= NewLayerHighlight;
                    ActiveChapterState[doc] = null;
                }
            }
            else
            {
                ActiveChapterState[doc] = result;
                VisualizerLayerWrappers.Highlight(ActiveChapterState[doc]);
                LayerChecker.LayerAddedEvent += NewLayerHighlight;
            }
        }
    }

}






