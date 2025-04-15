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
using System.IO;
using System.Reflection;
using LayerWorks.UI;
using HostMgd.ApplicationServices;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayerAlterer
    {
        private readonly Dictionary<string, string[]> _presentAuxData = new();
        private readonly object _syncObject = new();
        private readonly ILayerChecker _checker;
        private readonly IEntityFormatter _formatter;
        static LayerAlterer() { }

        public LayerAlterer(ILayerChecker checker, IEntityFormatter formatter)
        {
            _checker = checker;
            _formatter = formatter;
            UpdatePresentAuxData();
            Workstation.Redefined += (s, e) => UpdatePresentAuxData();
        }

        internal string PrevStatus { get; set; } = "";
        internal static Dictionary<string, string> PreviousAssignedData { get; } = new();

        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        public void TransparentOverlayToggle()
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

        private void CreateHatchOverXrefs(Transaction transaction, LayerTableRecord ltrec)
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
        public void LayerStatusChange()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    var parser = LayerWrapper.ActiveWrappers.FirstOrDefault()?.LayerInfo.ParentParser;
                    var workWrappers = LayerWrapper.ActiveWrappers.Where(w => w.LayerInfo.Prefix == parser?.Prefix).ToList();
                    if (!workWrappers.Any())
                    {
                        Workstation.Logger?.LogInformation("Нет подходящих объектов в наборе");
                        return;
                    }

                    parser!.ExtractSectionInfo<StatusSection>(out string[] statuses, out Func<string, string> descriptions);
                    string newStatus = GetStringKeyword(statuses, statuses.Select(s => descriptions(s)).ToArray(), $"Укажите статус объекта <{PrevStatus}>");
                    PrevStatus = descriptions(newStatus);

                    Workstation.Logger?.LogDebug("{ProcessingObject}: Выбран статус \"{Status}\"", nameof(LayerAlterer), newStatus);


                    workWrappers.ForEach(w => w.LayerInfo.SwitchStatus(newStatus));
                    workWrappers.ForEach(w => w.Push());
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

        public void NewStandardLayer()
        {
            IRepository<string, LayerProps> repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                //var layers = GetLayersFromEditorInput(repository);
                var layers = GetLayersFromWindowInput(repository);
                foreach (var layerName in layers)
                {
                    try
                    {
                        ObjectId layerId = _checker.Check(layerName);
                        Workstation.Database.Clayer = layerId;
                        CurrentLayerWrapper.DirectPush();
                    }
                    catch (WrongLayerException ex)
                    {
                        Workstation.Logger?.LogInformation(ex, "Текущий слой не принадлежит к списку обрабатываемых слоёв ({Exception})", ex.Message);
                        continue;
                    }
                    catch (System.Exception ex)
                    {
                        // TODO: отловить возможные ошибки по типам
                        Workstation.Logger?.LogError(ex, "{ProcessingObject}: Ошибка - {Exception}", nameof(LayerAlterer), ex.Message);
                        continue;
                    }
                }
                transaction.Commit();
            }

        }

        private static IEnumerable<string> GetLayersFromEditorInput(IRepository<string, LayerProps> repository)
        {
            // Выбрать парсер для загрузки слоёв
            string[] parserIds = NameParser.LoadedParsers.Keys.ToArray();
            string prefix = GetStringKeyword(parserIds, "Выберите глобальный классификатор");
            var workParser = NameParser.LoadedParsers[prefix];
            // Выбрать основной классификатор
            workParser.ExtractSectionInfo<PrimaryClassifierSection>(out string[] primaries, out Func<string, string> descriptions);
            string newLayerPrimary = GetStringKeyword(primaries, primaries.Select(p => descriptions(p)).ToArray(), "Выберите основной классификатор");
            // Выбрать ключи из репозитория, отфильтровать по выбранному классификатору и представить как массивы разделённых строк
            repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
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
            yield return layerName;
        }

        private static IEnumerable<string> GetLayersFromWindowInput(IRepository<string, LayerProps> repository)
        {
            var layers = repository.GetKeys();
            try
            {
                NewStandardLayerWindow window = new(layers);
                Application.ShowModalWindow(window);
                return window.GetResultLayers();
            }
            catch (Exception ex)
            {
                Workstation.Logger?.LogError(ex, "{ProcessingObject}: Ошибка - {Exception}", nameof(LayerAlterer), ex.Message);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        public void LayerAlter()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    var repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, string>>();
                    SelectionHandler.UpdateActiveLayerWrappers();
                    var parser = LayerWrapper.ActiveWrappers.FirstOrDefault()?.LayerInfo.ParentParser;
                    var workWrappers = LayerWrapper.ActiveWrappers.Where(w => w.LayerInfo.Prefix == parser?.Prefix).ToList();
                    if (!workWrappers.Any())
                    {
                        Workstation.Logger?.LogInformation("Нет подходящих объектов в наборе");
                        return;
                    }
                    foreach (LayerWrapper wrapper in workWrappers)
                    {
                        string initialMainName = wrapper.LayerInfo.MainName;
                        bool success = repository.TryGet(initialMainName, out string? newMainName);
                        if (success)
                        {
                            wrapper.LayerInfo.AlterMainName(newMainName!);
                            Workstation.Logger?.LogDebug("{ProcessingObject}:\tДля слоя \"{Layer}\" найден альтернативный слой \"{NewLayer}\"", nameof(LayerAlter), initialMainName, newMainName);
                        }
                        else
                        {
                            Workstation.Logger?.LogDebug("{ProcessingObject}:\tНе найден альтернативный слой для слоя \"{Layer}\"", nameof(LayerAlter), initialMainName);
                        }
                    }
                    workWrappers.ForEach(w => w.Push());
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
        public void LayerTag()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    NameParser? workParser = LayerWrapper.ActiveWrappers.FirstOrDefault()?.LayerInfo.ParentParser;
                    var workWrappers = LayerWrapper.ActiveWrappers.Where(w => w.LayerInfo.Prefix == workParser?.Prefix).ToList();
                    if (!workWrappers.Any())
                    {
                        Workstation.Logger?.LogInformation("Нет подходящих объектов в наборе");
                        return;
                    }

                    string[] suffixTags = workParser!.SuffixKeys.Keys.ToArray();
                    string[] descriptions = workParser.SuffixKeys.Values.ToArray();
                    string tag = GetStringKeyword(suffixTags, descriptions, "Выберите тип суффикса для отметки объекта");

                    bool targetValue = !workWrappers.First().LayerInfo.SuffixTagged[tag];
                    workWrappers.ForEach(l => l.LayerInfo.SwitchSuffix(tag, targetValue));
                    workWrappers.ForEach(l => l.Push());
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
        public void AuxDataAssign()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    NameParser? workParser = LayerWrapper.ActiveWrappers.FirstOrDefault()?.LayerInfo.ParentParser;
                    var workWrappers = LayerWrapper.ActiveWrappers.Where(w => w.LayerInfo.Prefix == workParser?.Prefix).ToList();
                    if (!workWrappers.Any())
                    {
                        Workstation.Logger?.LogInformation("Нет подходящих объектов в наборе");
                        return;
                    }

                    var dataSections = workParser!.AuxilaryDataKeys;
                    string dataKey = GetStringKeyword(dataSections.Keys.ToArray(), dataSections.Values.ToArray(), "Выберите тип дополнительных данных:");
                    if (!PreviousAssignedData.ContainsKey(dataKey))
                    {
                        PreviousAssignedData[dataKey] = "";
                    }
                    string previousAssignedData = PreviousAssignedData[dataKey];
                    // пробелы дают сбои, поэтому сопоставляем исходные с заменёнными выбираем заменённые и в работу через словарь передаём исходные
                    Dictionary<string, string> presentData = GetPresentAuxData(dataKey).Append("Сброс")
                                                                                       .ToDictionary(s => s.Replace(" ", "_"), s => s);

                    PromptKeywordOptions pko = new($"Введите значение [{string.Join(@"/", presentData.Keys)}]: <{previousAssignedData}>", $"{string.Join(@" ", presentData.Keys)}")
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
                            newData = presentData[result.StringResult];
                            PreviousAssignedData[dataKey] = newData;
                        }
                    }
                    else
                    {
                        return;
                    }

                    workWrappers.ForEach(w => w.LayerInfo.ChangeAuxilaryData(dataKey, newData));
                    workWrappers.ForEach(w => w.Push());

                    var layerNames = Workstation.Database.LayerTableId.GetObject<LayerTable>(OpenMode.ForRead, transaction)
                                                                      .Cast<ObjectId>()
                                                                      .Select(id => id.GetObject<LayerTableRecord>(OpenMode.ForRead, transaction))
                                                                      .Select(ltr => ltr.Name)
                                                                      .ToArray();
                    _ = Task.Run(() => UpdatePresentAuxData(dataKey, layerNames));

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
        public void StandartLayerValues()
        {
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
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

        public void StandartLayerHatch()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    SelectionHandler.UpdateActiveLayerWrappers();
                    var wrappers = LayerWrapper.ActiveWrappers.Where(w => w is EntityLayerWrapper)
                                                              .Select(w => (EntityLayerWrapper)w);
                    var modelSpace = Workstation.ModelSpace;
                    var drawOrderTable = modelSpace.DrawOrderTableId.GetObject<DrawOrderTable>(OpenMode.ForWrite, transaction);
                    foreach (var wrapper in wrappers)
                    {
                        wrapper.Push();
                        var polylines = wrapper.BoundEntities.Where(e => e is Polyline pl && pl.Closed).Select(e => (Polyline)e);
                        ObjectId[] polylineIds = polylines.Select(pl => pl.ObjectId).ToArray();
                        if (polylineIds.Length == 0)
                        {
                            Workstation.Logger?.LogInformation("В наборе отсутствуют замкнутые полилинии слоя \"{Layer}\". Пропуск объектов слоя", wrapper.LayerInfo.Name);
                            continue;
                        }
                        ObjectIdCollection plIdCollection = new(polylineIds);
                        Hatch hatch = new()
                        {
                            Layer = wrapper.LayerInfo.Name,
                            HatchStyle = HatchStyle.Normal
                        };
                        _formatter.FormatEntity(hatch, wrapper.LayerInfo.TrueName);

                        hatch.AssingnLoop(polylines);

                        if (hatch.PatternName != "")
                        {
                            Workstation.ModelSpace.AppendEntity(hatch);
                            transaction.AddNewlyCreatedDBObject(hatch, true);
                            var drawOrder = drawOrderTable.GetFullDrawOrder(0);
                            int minIndex = polylineIds.Min(p => drawOrder.IndexOf(p));
                            var pl = polylineIds.First(p => drawOrder.IndexOf(p) == minIndex);
                            drawOrderTable.MoveBelow(new ObjectIdCollection(new ObjectId[] { hatch.ObjectId }), pl);
                        }
                        else
                        {
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Нет стандарта штриховки для слоя {Layer}", nameof(StandartLayerHatch), wrapper.LayerInfo.Name);
                        }
                    }
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

        private string[] GetPresentAuxData(string key)
        {
            lock (_syncObject)
            {
                bool success = _presentAuxData.TryGetValue(key, out string[]? data);
                if (success)
                    return data!;
                else
                    return Array.Empty<string>();
            }
        }

        private void UpdatePresentAuxData(string key, IEnumerable<string> layerNames)
        {
            string[] names = layerNames.Select(n => NameParser.ParseLayerName(n))
                                       .Where(r => r.Status == LayerInfoParseStatus.Success)
                                       .Select(r => r.Value!.AuxilaryData[key])
                                       .Where(s => !string.IsNullOrEmpty(s))
                                       .Select(s => s!)
                                       .Distinct()
                                       .OrderBy(s => s)
                                       .ToArray();
            lock (_syncObject)
            {
                _presentAuxData[key] = names;
            }
        }

        private void UpdatePresentAuxData()
        {
            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                var keys = NameParser.Current.AuxilaryDataKeys.Keys;
                if (!keys.Any())
                {
                    transaction.Abort();
                    return;
                }
                string[] layerNames = Workstation.Database.LayerTableId.GetObject<LayerTable>(OpenMode.ForRead, transaction)
                                                                       .Cast<ObjectId>()
                                                                       .Select(id => id.GetObject<LayerTableRecord>(OpenMode.ForRead, transaction))
                                                                       .Select(ltr => ltr.Name)
                                                                       .ToArray();
                foreach (var key in keys)
                {
                    UpdatePresentAuxData(key, layerNames);
                }
            }
        }
    }

}






