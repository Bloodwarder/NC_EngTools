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
using LayerWorks.DataRepositories;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using NameClassifiers;
using NameClassifiers.Sections;
using NanocadUtilities;

using static NanocadUtilities.EditorHelper;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayersCommands
    {
        private const string ExtProjectAuxDataKey = "ExternalProject";
        internal static string PrevStatus = "Сущ";
        internal static Dictionary<string, string> PreviousAssignedData = new();
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        [CommandMethod("КАЛЬКА")]
        public static void TransparentOverlayToggle()
        {
            Workstation.Define();
            string tgtlayer = LayerWrapper.StandartPrefix + "_Калька";

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
        // TODO: Изменить команду для работы с xml данными из NameParser
        /// <summary>
        /// Изменение статуса объекта в соответствии с данными LayerParser
        /// </summary>
        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public static void LayerStatusChange()
        {
            Workstation.Define();

            NameParser.LoadedParsers[LayerWrapper.StandartPrefix!].ExtractSectionInfo<StatusSection>(out string[] statuses,
                                                                                                     out Func<string, string> descriptions);
            string newStatus = GetStringKeywordResult(statuses, statuses.Select(s => descriptions(s)).ToArray(), $"Укажите статус объекта <{PrevStatus}>");

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info => info.SwitchStatus(newStatus)));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                catch (System.Exception ex)
                {
                    Workstation.Editor.WriteMessage(ex.Message);
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        [CommandMethod("НОВСТАНДСЛОЙ")]
        public static void NewStandardLayer()
        {
            Workstation.Define();

            // Выбрать парсер для загрузки слоёв
            string[] parserIds = NameParser.LoadedParsers.Keys.ToArray();
            string prefix = GetStringKeywordResult(parserIds, "Выберите глобальный классификатор");
            var workParser = NameParser.LoadedParsers[prefix];
            // Выбрать основной классификатор
            workParser.ExtractSectionInfo<PrimaryClassifierSection>(out string[] primaries, out Func<string, string> descriptions);
            string newLayerPrimary = GetStringKeywordResult(primaries, primaries.Select(p => descriptions(p)).ToArray(), "Выберите основной классификатор");
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
                string selector = GetStringKeywordResult(selectors, "Выберите классификатор");
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
                    Workstation.Editor.WriteMessage($"Cлой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                catch (System.Exception ex)
                {
                    Workstation.Editor.WriteMessage(ex.Message);
                }
            }

        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public static void LayerAlter()
        {
            Workstation.Define();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info =>
                    {
                        bool success = NcetCore.ServiceProvider.GetRequiredService<InMemoryLayerAlterRepository>()
                                                               .TryGet(info.MainName, out string? name);
                        if (success)
                            info.AlterSecondaryClassifier(name!);
                        return;
                    }));
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
        /// Назначение объекту/слою приписки, обозначающей переустройство
        /// </summary>
        [CommandMethod("ПЕРЕУСТРОЙСТВО", CommandFlags.Redraw)]
        public static void LayerReconstruction()
        {
            Workstation.Define();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();

                    bool targetValue = !ActiveLayerWrappers.List.FirstOrDefault()!.LayerInfo.SuffixTagged["Reconstruction"];
                    ActiveLayerWrappers.List.ForEach(l => l.AlterLayerInfo(info => { info.SuffixTagged["Reconstruction"] = targetValue; }));
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
        /// Назначение объекту/слою имени внешнего проекта (неутверждаемого)
        /// </summary>
        [CommandMethod("ДОПИНФО", CommandFlags.Redraw)]
        public static void ExtAssign()
        {
            Workstation.Define();

            NameParser workParser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
            var dataSections = workParser.AuxilaryDataKeys;
            string dataKey = GetStringKeywordResult(dataSections.Keys.ToArray(), dataSections.Values.ToArray(), "Выберите тип дополнительных данных:");
            if (!PreviousAssignedData.ContainsKey(dataKey))
            {
                PreviousAssignedData[dataKey] = "";
            }
            PromptStringOptions pso = new($"Введите значение:")
            {
                AllowSpaces = true,
                DefaultValue = PreviousAssignedData[dataKey],
                UseDefaultValue = true,
            };
            PromptResult result = Workstation.Editor.GetString(pso);
            string newData;
            if (result.Status != (PromptStatus.Error | PromptStatus.Cancel))
            {
                newData = result.StringResult;
                PreviousAssignedData[dataKey] = newData;
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
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info => info.ChangeAuxilaryData(dataKey, newData)));
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
        [CommandMethod("СВС", CommandFlags.Redraw)]
        public static void StandartLayerValues()
        {
            Workstation.Define();
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
        [CommandMethod("ПРЕФИКС")]
        public void ChangePrefix()
        {
            Workstation.Define();
            List<string> additionalOptions = new()
            {
                "Переопределить",
                "Загрузить"
            };
            List<string> prefixes = NameParser.LoadedParsers.Keys.OrderBy(k => k).Concat(additionalOptions).ToList();

            PromptKeywordOptions pko = new($"Выберите префикс обрабатываемых слоёв <{LayerWrapper.StandartPrefix}> [{string.Join("/", prefixes)}", string.Join(" ", prefixes))
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult result = Workstation.Editor.GetKeywords(pko);
            if (result.Status != PromptStatus.OK)
                return;
            if (result.StringResult == "Переопределить")
            {
                RedefinePrefix();
                return;
            }
            if (result.StringResult == "Загрузить")
            {
                LoadNewParser();
                return;
            }
            string newprefix = result.StringResult;
            LayerWrapper.StandartPrefix = newprefix;
        }

        private void RedefinePrefix()
        {
            throw new NotImplementedException();
        }

        private void LoadNewParser()
        {
            throw new NotImplementedException();
        }
    }

}






