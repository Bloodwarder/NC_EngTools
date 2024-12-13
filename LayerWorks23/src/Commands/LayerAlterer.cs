//System
//nanoCAD
using HostMgd.EditorInput;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;
//Microsoft
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//internal modules
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore;
using LoaderCore.Interfaces;
using NameClassifiers;
using NameClassifiers.Sections;
using LoaderCore.NanocadUtilities;

using static LoaderCore.NanocadUtilities.EditorHelper;
using System.Globalization;
using LayerWorks.EntityFormatters;
using System.IO;
using System;
using System.Reflection;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayerAlterer
    {
        internal static string PrevStatus = "";
        private static ILayerChecker _checker = NcetCore.ServiceProvider.GetRequiredService<ILayerChecker>();
        static LayerAlterer()
        {
        }

        internal static Dictionary<string, string> PreviousAssignedData { get; } = new();
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        public static void TransparentOverlayToggle()
        {
            string tgtlayer = NameParser.Current.Prefix + "_Калька";

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Получение таблицы слоёв", nameof(LayerAlterer));
                LayerTable? lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForWrite, false) as LayerTable;
                if (!lt!.Has(tgtlayer))
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Слой {OverlayLayer} отсутствует в чертеже. Добавление", nameof(LayerAlterer), tgtlayer);

                    LayerTableRecord ltrec = new()
                    {
                        Name = tgtlayer,
                        Color = Color.FromRgb(255, 255, 255),
                    };
                    lt.Add(ltrec);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Слой {OverlayLayer} добавлен", nameof(LayerAlterer), tgtlayer);
                    transaction.AddNewlyCreatedDBObject(ltrec, true);
                    CreateHatchOverXrefs(transaction, ltrec);
                }
                else
                {
                    //LayerTableRecord overlayLayer = (LayerTableRecord)transaction.GetObject(lt[tgtlayer], OpenMode.ForWrite, false);
                    LayerTableRecord overlayLayer = lt[tgtlayer].GetObject<LayerTableRecord>(OpenMode.ForWrite);
                    if (overlayLayer!.IsFrozen || overlayLayer.IsOff)
                    {
                        overlayLayer.IsOff = false;
                        overlayLayer.IsFrozen = false;
                    }
                    else
                    {
                        overlayLayer.IsOff = true;
                    }
                }
                transaction.Commit();
                Workstation.Logger?.LogDebug("{ProcessingObject}: Транзакция завершена. Изменения применены", nameof(LayerAlterer));
            }
        }

        private static void CreateHatchOverXrefs(Transaction transaction, LayerTableRecord ltrec)
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало процедуры создания кальки поверх внешних ссылок", nameof(LayerAlterer));
            // Получить системные переменные, необходимые для вычисления размеров кальки
            var viewCenter = (Point3d)HostMgd.ApplicationServices.Application.GetSystemVariable("VIEWCTR");
            var viewSize = (double)HostMgd.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");
            var screenSize = (Point2d)HostMgd.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");

            Workstation.Logger?.LogDebug(
                "{ProcessingObject}: Системные переменные для расчёта:\nVIEWCTR :\t{ViewCenter}\nVIEWSIZE:\t{ViewSize}\nSCREENSIZE:\t{ScreenSize}",
                nameof(LayerAlterer),
                viewCenter,
                viewSize,
                screenSize);

            double multiplier = viewSize / screenSize.Y;
            Point2d minExtent = new(viewCenter.X - screenSize.X * multiplier / 2, viewCenter.Y - screenSize.Y * multiplier / 2);
            Point2d maxExtent = new(viewCenter.X + screenSize.X * multiplier / 2, viewCenter.Y + screenSize.Y * multiplier / 2);
            Workstation.Logger?.LogDebug(
                "{ProcessingObject}: Полученные значения охвата:\nЛевая нижняя точка:\t{MinExtent}\nПравая верхняя точка:\t{MaxExtent}",
                nameof(LayerAlterer),
                minExtent.ToString(CultureInfo.InvariantCulture),
                maxExtent.ToString(CultureInfo.InvariantCulture));
            Polyline pl = new();
            pl.AddVertexAt(0, new(minExtent.X, minExtent.Y), 0, 0, 0);
            pl.AddVertexAt(1, new(minExtent.X, maxExtent.Y), 0, 0, 0);
            pl.AddVertexAt(2, new(maxExtent.X, maxExtent.Y), 0, 0, 0);
            pl.AddVertexAt(3, new(maxExtent.X, minExtent.Y), 0, 0, 0);

            pl.Closed = true;
            pl.LayerId = ltrec.Id;
            Hatch hatch = new()
            {
                LayerId = ltrec.Id
            };
            Workstation.ModelSpace.AppendEntity(pl);
            transaction.AddNewlyCreatedDBObject(pl, true);
            hatch.AppendLoop(HatchLoopTypes.Polyline, new ObjectIdCollection(new ObjectId[] { pl.Id }));
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            Workstation.ModelSpace.AppendEntity(hatch);
            transaction.AddNewlyCreatedDBObject(hatch, true);
            Workstation.Logger?.LogDebug("{ProcessingObject}: Объекты кальки добавлены в ModelSpace", nameof(LayerAlterer));

            DrawOrderTable dot = (DrawOrderTable)transaction.GetObject(Workstation.ModelSpace.DrawOrderTableId, OpenMode.ForWrite);

            // Выбрать все внешние сслыки в модели
            Func<DBObject, bool> isFromExternalReferencePredicate =
                dbo => ((BlockTableRecord)transaction.GetObject(((BlockReference)dbo).BlockTableRecord, OpenMode.ForRead)).IsFromExternalReference;
            var xRefsIds = Workstation.ModelSpace.Cast<ObjectId>()
                                                 .Select(id => transaction.GetObject(id, OpenMode.ForRead))
                                                 .Where(dbo => dbo is BlockReference)
                                                 .Where(isFromExternalReferencePredicate)
                                                 .Select(dbo => dbo.ObjectId)
                                                 .ToArray();
            if (!xRefsIds.Any())
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Внешние ссылки отсутствуют. Перемещаем кальку в самый низ", nameof(LayerAlterer));
                dot.MoveToBottom(new() { pl.Id, hatch.Id });
                return;
            }
            Workstation.Logger?.LogDebug("{ProcessingObject}: Найдено {Count} внешних ссылок. Перемещаем кальку над ссылками", nameof(LayerAlterer), xRefsIds.Length);
            ObjectIdCollection xreferences = new(xRefsIds);
            // Сравнить индексы внешних ссылок и поднять кальку над самой верхней
            var drawOrder = dot.GetFullDrawOrder(0);
            var indexes = xRefsIds.ToDictionary(id => id, id => drawOrder.IndexOf(id));
            var firstXRef = xRefsIds.Where(id => indexes[id] == indexes.Values.Max()).First();
            dot.MoveAbove(new ObjectIdCollection(new ObjectId[] { pl.ObjectId, hatch.ObjectId }), firstXRef);
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
                    LayerWrapper.ActiveWrappers.ForEach(w => w.LayerInfo.SwitchStatus(newStatus));
                    LayerWrapper.ActiveWrappers.ForEach(w => w.Push());
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
                    LayerWrapper.ActiveWrappers.Clear();
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
                                           .Where(s => s.StartsWith($"{prefix}{workParser.Separator}{newLayerPrimary}{workParser.Separator}"))
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

            string layerName = $"{string.Join(workParser.Separator, keysArray.First())}";
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    ObjectId layerId = _checker.Check(layerName);
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
                    foreach (LayerWrapper wrapper in LayerWrapper.ActiveWrappers)
                    {
                        bool success = repository.TryGet(wrapper.LayerInfo.MainName, out string? newMainName);
                        if (success)
                            wrapper.LayerInfo.AlterSecondaryClassifier(newMainName!);
                    }

                    LayerWrapper.ActiveWrappers.ForEach(w => w.Push());
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
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
                    bool targetValue = !LayerWrapper.ActiveWrappers.First().LayerInfo.SuffixTagged[tag];
                    LayerWrapper.ActiveWrappers.ForEach(l => l.LayerInfo.SwitchSuffix(tag, targetValue));
                    LayerWrapper.ActiveWrappers.ForEach(l => l.Push());
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
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
                    LayerWrapper.ActiveWrappers.ForEach(w => w.LayerInfo.ChangeAuxilaryData(dataKey, newData));
                    LayerWrapper.ActiveWrappers.ForEach(w => w.Push());
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Message})", ex.Message);
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
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
                    LayerWrapper.ActiveWrappers.ForEach(w => w.Push());
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Message})", ex.Message);
                }
                finally
                {
                    LayerWrapper.ActiveWrappers.Clear();
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
            FileInfo assemblyPath = new(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo dir = new(Path.Combine(assemblyPath.Directory!.Parent!.Parent!.FullName, "UserData"));
            var parserXmlFiles = dir.GetFiles("LayerParser_*.xml")
                                    .ToArray();
            string parser = GetStringKeyword(parserXmlFiles.Select(f => f.Name).ToArray(), "Выберите парсер для загрузки");
            var loadingParser = parserXmlFiles.Where(f => f.Name == parser).Single();
            NameParser.Load(loadingParser.FullName);
            Workstation.Logger?.LogInformation("Парсер {Parser} загружен", loadingParser.Name);
        }
    }

}






