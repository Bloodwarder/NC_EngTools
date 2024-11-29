//System

//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
//internal modules
using LayerWorks.LayerProcessing;
using NameClassifiers;
using NameClassifiers.Highlighting;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;
using static LoaderCore.NanocadUtilities.EditorHelper;
using Microsoft.Extensions.Logging;
using LayerWorks.HighlightFiltering;

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
        public static void Visualizer()
        {
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;
            Document doc = Workstation.Document;
            Database db = Workstation.Database;

            string? prefix = NameParser.Current.Prefix;
            if (prefix == null)
            {
                Workstation.Logger?.LogInformation("Не задан префикс слоёв для выполнения команды");
                return;
            }

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = (LayerTable)transaction.GetObject(db.LayerTableId, OpenMode.ForRead, false);
                var layers = (from ObjectId elem in lt
                             let ltr = tm.GetObject(elem, OpenMode.ForWrite, false) as LayerTableRecord
                             where ltr.Name.StartsWith(NameParser.Current.Prefix + NameParser.Current.Separator)
                             select ltr).ToArray();
                if (layers.Length == 0)
                {
                    Workstation.Logger?.LogInformation("Нет подходящих слоёв. Завершение команды");
                    return;
                }
                int errorCount = 0;
                int successCount = 0;
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        VisualizerLayerWrapper.Create(ltr); // BUG: Это место тормозит. Выяснить. Тормозит только при включенной отладке
                        successCount++;
                    }
                    catch (WrongLayerException)
                    {
                        errorCount++;
                        continue;
                    }
                }
                Workstation.Logger?.LogInformation("Фильтр включен для {SuccessCount} слоёв. Число необработанных слоёв: {ErrorCount}", successCount, errorCount);

                Visualizers visualizers = NameParser.Current.Visualizers;
                string[] filters = visualizers.Filters.Select(x => x.Name).ToArray();
                string filterName = GetStringKeyword(filters, "Выберите фильтр");
                HighlightFilter chosenFilter = visualizers.Filters.Where(f => f.Name == filterName).Single();

                //var states = VisualizerLayerWrappers.StoredLayerStates[Workstation.Document];
                //chosenFilter.ApplyFilter(states); //ПЕРЕСОБРАТЬ С ЭТИМ И ТЕСТИРОВАТЬ

                // UNDONE: Работает по-старому! Собрано просто, чтобы было не сломано. Взять логику из визуализаторов ^^^
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


        internal static void NewLayerHighlight(object? sender, EventArgs e)
        {
            Document doc = Workstation.Document;

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                VisualizerLayerWrapper cslp = new((LayerTableRecord)sender!);
                cslp.Push(ActiveChapterState[doc], new() { "пр", "неутв" });
                transaction.Commit();
            }

        }
        private static void ApplyVisualizer(Document doc, string result)
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






