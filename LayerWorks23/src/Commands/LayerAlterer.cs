﻿//System
//nanoCAD
using HostMgd.EditorInput;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Runtime;
//Microsoft
using Microsoft.Extensions.DependencyInjection;
//internal modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using NameClassifiers;
using NameClassifiers.Sections;
using LoaderCore.NanocadUtilities;

using static LoaderCore.NanocadUtilities.EditorHelper;
using Microsoft.Extensions.Logging;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayerAlterer
    {
        internal static string PrevStatus = "";
        internal static Dictionary<string, string> PreviousAssignedData { get; } = new();
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        public static void TransparentOverlayToggle()
        {
            string tgtlayer = NameParser.Current.Prefix + "_Калька";

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForWrite, false) as LayerTable;
                if (!lt!.Has(tgtlayer))
                {
                    LayerTableRecord ltrec = new()
                    {
                        Name = tgtlayer,
                        Color = Color.FromRgb(255, 255, 255),
                    };
                    lt.Add(ltrec);
                    transaction.AddNewlyCreatedDBObject(ltrec, true);
                    CreateHatchOverXrefs(transaction, ltrec);
                }
                else
                {
                    LayerTableRecord? ltrec = (from ObjectId elem in lt
                                               let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForWrite, false)
                                               where ltr.Name == tgtlayer
                                               select ltr)
                                              .FirstOrDefault();
                    if (ltrec!.IsFrozen || ltrec.IsOff)
                    {
                        ltrec.IsOff = false;
                        ltrec.IsFrozen = false;
                    }
                    else
                    {
                        ltrec.IsOff = true;
                    }
                }
                transaction.Commit();
            }
        }

        private static void CreateHatchOverXrefs(Transaction transaction, LayerTableRecord ltrec)
        {
            Extents3d? extents3D = Workstation.Editor.GetCurrentView().Bounds;
            if (extents3D is null)
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Охват текущего вида не определён", nameof(LayerAlterer));
            }
            else
            {
                Polyline pl = new();
                pl.AddVertexAt(0, new(extents3D.Value.MinPoint.X, extents3D.Value.MinPoint.Y), 0, 0, 0);
                pl.AddVertexAt(1, new(extents3D.Value.MinPoint.X, extents3D.Value.MaxPoint.Y), 0, 0, 0);
                pl.AddVertexAt(2, new(extents3D.Value.MaxPoint.X, extents3D.Value.MaxPoint.Y), 0, 0, 0);
                pl.AddVertexAt(3, new(extents3D.Value.MaxPoint.X, extents3D.Value.MinPoint.Y), 0, 0, 0);
                pl.Closed = true;
                pl.LayerId = ltrec.Id;
                Hatch hatch = new();
                hatch.LayerId = ltrec.Id;
                hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(new ObjectId[] { pl.Id }));
                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");

                DrawOrderTable dot = (DrawOrderTable)transaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);
                Func<DBObject, bool> isFromExternalReferencePredicate =
                    dbo => ((BlockTableRecord)transaction.GetObject(((BlockReference)dbo).BlockTableRecord, OpenMode.ForRead)).IsFromExternalReference;
                var xRefsIds = Workstation.ModelSpace.Cast<ObjectId>()
                                                     .Select(id => transaction.GetObject(id, OpenMode.ForRead))
                                                     .Where(dbo => dbo is BlockReference)
                                                     .Where(isFromExternalReferencePredicate)
                                                     .Select(dbo => dbo.ObjectId)
                                                     .ToArray();
                ObjectIdCollection xreferences = new ObjectIdCollection(xRefsIds);
                var drawOrder = dot.GetFullDrawOrder(0);
                var indexes = xRefsIds.ToDictionary(id => id, id => drawOrder.IndexOf(id));
                var firstXRef = xRefsIds.Where(id => indexes[id] == indexes.Values.Min()).First();
                dot.MoveAbove(new ObjectIdCollection(new ObjectId[] { pl.ObjectId, hatch.ObjectId }), firstXRef);
            }
        }

        /// <summary>
        /// Изменение статуса объекта в соответствии с данными LayerParser
        /// </summary>
        public static void LayerStatusChange()
        {
            NameParser.Current.ExtractSectionInfo<StatusSection>(out string[] statuses, out Func<string, string> descriptions);
            string newStatus = GetStringKeyword(statuses, statuses.Select(s => descriptions(s)).ToArray(), $"Укажите статус объекта <{PrevStatus}>");
            PrevStatus = descriptions(newStatus);

            Workstation.Logger?.LogDebug("{ProcessingObject}: Выбран статус \"{Status}\"", nameof(LayerAlterer), newStatus);

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    ActiveLayerWrappers.List.ForEach(w => w.LayerInfo.SwitchStatus(newStatus));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                catch (System.Exception ex)
                {
                    Workstation.Logger?.LogError(ex, "{ProcessingObject}: Ошибка - {Exception}", nameof(LayerAlterer), ex.Message);

                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        public static void NewStandardLayer()
        {
            // Выбрать парсер для загрузки слоёв
            string[] parserIds = NameParser.LoadedParsers.Keys.ToArray();
            string prefix = GetStringKeyword(parserIds, "Выберите глобальный классификатор");
            var workParser = NameParser.LoadedParsers[prefix];
            // Выбрать основной классификатор
            workParser.ExtractSectionInfo<PrimaryClassifierSection>(out string[] primaries, out Func<string, string> descriptions);
            string newLayerPrimary = GetStringKeyword(primaries, primaries.Select(p => descriptions(p)).ToArray(), "Выберите основной классификатор");
            // Выбрать ключи из репозитория, отфильтровать по выбранному классификатору и представить как массивы разделённых строк
            IRepository<string, LayerProps> repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
            string[][] keysArray = repository.GetKeys()
                                           .Where(s => s.StartsWith($"{newLayerPrimary}{workParser.Separator}"))
                                           .Select(s => s.Split(workParser.Separator))
                                           .ToArray();
            // Последовательно запрашивать у пользователя строку для следующего уровня фильтрации, пока не останется 1 объект
            int pointer = 1; // Потому что первичный классификатор уже обработан
            while (keysArray.First().Length >= pointer && keysArray.Length > 1)
            {
                string[] selectors = keysArray.Select(k => k[pointer]).Distinct().ToArray();
                string selector = GetStringKeyword(selectors, "Выберите классификатор");
                keysArray = keysArray.Where(s => s[pointer] == selector).ToArray();
                pointer++;
            }

            string layerName = $"{workParser.Prefix}{workParser.Separator}{string.Join(workParser.Separator, keysArray.First())}";
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    ObjectId layerId = LayerChecker.Check(layerName);
                    Workstation.Database.Clayer = layerId;
                    CurrentLayerWrapper.DirectPush();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                catch (System.Exception ex)
                {
                    Workstation.Logger?.LogError(ex, "{ProcessingObject}: Ошибка - {Exception}", nameof(LayerAlterer), ex.Message);
                }
            }

        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        public static void LayerAlter()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    var repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, string>>();
                    SelectionHandler.UpdateActiveLayerWrappers();
                    foreach (LayerWrapper wrapper in ActiveLayerWrappers.List)
                    {
                        bool success = repository.TryGet(wrapper.LayerInfo.MainName, out string? newMainName);
                        if (success)
                            wrapper.LayerInfo.AlterSecondaryClassifier(newMainName!);
                    }

                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою тега с определённым значением (BooleanSuffix)
        /// </summary>
        public static void LayerTag()
        {
            Workstation.Define();
            NameParser workParser = NameParser.Current;
            string[] suffixTags = workParser.SuffixKeys.Keys.ToArray();
            string[] descriptions = workParser.SuffixKeys.Values.ToArray();
            string tag = GetStringKeyword(suffixTags, descriptions, "Выберите тип суффикса для отметки объекта");
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    bool targetValue = !ActiveLayerWrappers.List.First().LayerInfo.SuffixTagged[tag];
                    ActiveLayerWrappers.List.ForEach(l => l.LayerInfo.SwitchSuffix(tag, targetValue));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою имени внешнего проекта (неутверждаемого)
        /// </summary>
        public static void AuxDataAssign()
        {
            NameParser workParser = NameParser.Current;
            var dataSections = workParser.AuxilaryDataKeys;
            string dataKey = GetStringKeyword(dataSections.Keys.ToArray(), dataSections.Values.ToArray(), "Выберите тип дополнительных данных:");
            if (!PreviousAssignedData.ContainsKey(dataKey))
            {
                PreviousAssignedData[dataKey] = "";
            }
            string previousAssignedData = PreviousAssignedData[dataKey];
            PromptKeywordOptions pko = new($"Введите значение [{previousAssignedData}/Сброс]: <{previousAssignedData}>", $"{previousAssignedData} Сброс")
            {
                AllowArbitraryInput = true,
                AllowNone = true,
                AppendKeywordsToMessage = true
            };
            PromptResult result = Workstation.Editor.GetKeywords(pko);

            string? newData;
            if (result.Status == PromptStatus.None)
            {
                if (previousAssignedData != "")
                {
                    newData = previousAssignedData;
                }
                else
                {
                    newData = null;
                }
            }
            else if (result.Status != PromptStatus.Error && result.Status != PromptStatus.Cancel)
            {
                if (result.StringResult == "Сброс" || result.StringResult == "")
                {
                    newData = null;
                    PreviousAssignedData[dataKey] = "";
                }
                else
                {
                    newData = result.StringResult;
                    PreviousAssignedData[dataKey] = newData;
                }
            }
            else
            {
                return;
            }

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    ActiveLayerWrappers.List.ForEach(w => w.LayerInfo.ChangeAuxilaryData(dataKey, newData));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Приведение свойств объекта или текущих переменных чертежа к стандарту (ширина и масштаб типов линий)
        /// </summary>
        public static void StandartLayerValues()
        {
            TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }

        }

        /// <summary>
        /// Изменение обрабатываемого префикса слоёв
        /// </summary>
        public static void ChangePrefix()
        {
            List<string> additionalOptions = new()
            {
                "Переопределить",
                "Загрузить"
            };
            List<string> prefixes = NameParser.LoadedParsers.Keys.OrderBy(k => k).Concat(additionalOptions).ToList();

            PromptKeywordOptions pko = new($"Выберите префикс обрабатываемых слоёв <{NameParser.Current.Prefix}> [{string.Join("/", prefixes)}", string.Join(" ", prefixes))
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult result = Workstation.Editor.GetKeywords(pko);
            if (result.Status != PromptStatus.OK)
                return;
            switch (result.StringResult)
            {
                case "Переопределить":
                    RedefinePrefix();
                    return;
                case "Загрузить":
                    LoadNewParser();
                    return;
            }
            string newprefix = result.StringResult;
            NameParser.SetCurrentParser(newprefix);
        }

        private static void RedefinePrefix()
        {
            throw new NotImplementedException();
        }

        private static void LoadNewParser()
        {
            throw new NotImplementedException();
        }
    }

}






