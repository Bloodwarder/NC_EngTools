//System
using System.Text;
//Microsoft
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//nanoCAD
using HostMgd.EditorInput;
using Teigha.Colors;
using Teigha.DatabaseServices;
using Teigha.Geometry;
//internal modules
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using NameClassifiers;
using LayersIO.DataTransfer;
using static LoaderCore.NanocadUtilities.EditorHelper;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс для автосборки условных обозначений на основе слоёв чертежа и логики LayerParser
    /// </summary>
    public class LegendAssembler
    {
        private static readonly string[] _modeKeywords = { "Все", "Полигон" };

        /// <summary>
        /// Автосборка условных обозначений на основе слоёв чертежа и логики LayerParser
        /// </summary>
        public static void Assemble()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Запуск автосборки", nameof(LegendAssembler));
            //получить точку вставки
            PromptPointOptions ppo = new("Укажите точку вставки")
            {
                UseBasePoint = false,
                AllowNone = false
            };
            PromptPointResult pointResult = Workstation.Editor.GetPoint(ppo);
            if (pointResult.Status != PromptStatus.OK)
            {
                Workstation.Logger?.LogInformation("Точка не назначена. Команда отменена");
                return;
            }
            Point3d p3d = pointResult.Value;

            Workstation.Logger?.LogDebug("{ProcessingObject}: Точка назначена - X:{XCoord}, Y:{YCoord}", nameof(LegendAssembler), p3d.X, p3d.Y);
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало транзакции", nameof(LegendAssembler));

            //получить таблицу слоёв и слои
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Проверка наличия слоя для текста условных обозначений", nameof(LegendAssembler));
                LayerChecker.ForceCheck(string.Concat(NameParser.Current.Prefix, "_Условные"));
                Workstation.Database.Cecolor = Color.FromColorIndex(ColorMethod.ByLayer, 256);

                // Выбрать режим поиска слоёв (весь чертёж или в границах полилинии)
                Workstation.Logger?.LogDebug("{ProcessingObject}: Выбор режима поиска слоёв", nameof(LegendAssembler));

                string layerSearchMode = GetStringKeyword(_modeKeywords, "Выберите режим поиска слоёв:");

                Workstation.Logger?.LogDebug("{ProcessingObject}: Выбран режим \"{SearchMode}\"", nameof(LegendAssembler), layerSearchMode);

                int modeIndex = Array.IndexOf(_modeKeywords, layerSearchMode);
                // Получить записи таблицы слоёв в зависимости от режима поиска
                List<LayerTableRecord> layers = new();
                switch (modeIndex)
                {
                    case 0:
                        layers.AddRange(GetAllLayerTableRecords());
                        break;
                    case 1:
                        layers.AddRange(GetLayerTableRecordsByBounds());
                        break;
                }
                Workstation.Logger?.LogDebug("{ProcessingObject}: Выбрано {FoundLayers} слоёв", nameof(LegendAssembler), layers.Count);

                // Создать парсеры для слоёв
                StringBuilder wrongLayersStringBuilder = new();
                List<RecordLayerWrapper> layersList = new();
                var repository = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LegendData>>();
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        Workstation.Logger?.LogDebug("{ProcessingObject}: Создание RecordLayerWrapper для слоя {Layer}", nameof(LegendAssembler), ltr.Name);
                        RecordLayerWrapper rlp = new(ltr);
                        if (!repository.Has(rlp.LayerInfo.MainName))
                        {
                            string wrongLayerLine = $"Нет данных для слоя {rlp.LayerInfo.Prefix}{rlp.LayerInfo.ParentParser.Separator}{rlp.LayerInfo.MainName}";
                            Workstation.Logger?.LogDebug("{ProcessingObject}: {WrongLayer}", nameof(LegendAssembler), wrongLayerLine);
                            wrongLayersStringBuilder.AppendLine(wrongLayerLine);
                            continue;
                        }
                        layersList.Add(rlp);
                    }
                    catch (WrongLayerException ex)
                    {
                        Workstation.Logger?.LogDebug(ex, "{ProcessingObject}: Слой {Layer} не обработан. {Error}", nameof(LegendAssembler), ltr.Name, ex.Message);
                        wrongLayersStringBuilder.AppendLine($"Слой {ltr.Name} не обработан. {ex.Message}");
                        continue;
                    }
                }

                if (!layersList.Any())
                {
                    Workstation.Logger?.LogInformation("Нет подходящих слоёв. Завершение команды");
                    return;
                }

                // Создать шаблон ячейки для каждого успешно обработанного слоя
                List<LegendGridCell> cells = new();
                foreach (RecordLayerWrapper rlp in layersList)
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Создание LegendGridCell для слоя {Layer}", nameof(LegendAssembler), rlp.BoundLayer.Name);
                    cells.Add(new LegendGridCell(rlp));
                }
                // Выбрать фильтр (режим компоновки)
                string[][]? filterKeywords = NameParser.Current
                                                       .GlobalFilters
                                                       .GetFilterKeyWords(out int filtersCount);
                string[] chosenKeywords = new string[filtersCount];
                if (filterKeywords != null)
                {
                    for (int i = 0; i < filtersCount; i++)
                    {
                        try
                        {
                            chosenKeywords[i] = GetStringKeyword(filterKeywords[i], "Выберите режим компоновки:");
                        }
                        catch (System.Exception ex)
                        {
                            Workstation.Logger?.LogError(ex, "Ошибка выбора фильтра: {Error}", ex.Message);
                            return;
                        }
                    }
                }
                // Запустить компоновщик таблиц условных и получить созданные объекты чертежа для вставки в ModelSpace
                Workstation.Logger?.LogDebug("{ProcessingObject}: Выбранные фильтры {FilterKeywords}", nameof(LegendAssembler), string.Join(", ", chosenKeywords));
                List<Entity> entitiesList;
                Workstation.Logger?.LogDebug("{ProcessingObject}: Создание компоновщика сеток GridComposer", nameof(LegendAssembler));

                using (GridsComposer composer = new(cells, chosenKeywords))
                {
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Компоновка сеток", nameof(LegendAssembler));

                    composer.Compose(p3d);

                    Workstation.Logger?.LogDebug("{ProcessingObject}: Отрисовка сеток", nameof(LegendAssembler));

                    entitiesList = composer.DrawGrids();
                }
                // Получить таблицу блоков и ModelSpace, затем вставить объекты таблиц условных в чертёж
                BlockTableRecord modelSpace = Workstation.ModelSpace;
                Workstation.Database.Cecolor = Color.FromColorIndex(ColorMethod.ByLayer, 256);

                Workstation.Logger?.LogDebug("{ProcessingObject}: Добавление объектов в чертёж", nameof(LegendAssembler));

                foreach (Entity e in entitiesList)
                {
                    modelSpace!.AppendEntity(e);
                    transaction.AddNewlyCreatedDBObject(e, true); // и в транзакцию
                }
                // Поднять наверх в таблице порядка прорисовки все созданные объекты кроме штриховок
                Workstation.Logger?.LogDebug("{ProcessingObject}: Подъём объектов над штриховками в таблице отрисовки", nameof(LegendAssembler));

                DrawOrderTable drawOrderTable = (DrawOrderTable)transaction.GetObject(modelSpace!.DrawOrderTableId, OpenMode.ForWrite);
                drawOrderTable.MoveToTop(new ObjectIdCollection(entitiesList.Where(e => e is not Hatch).Select(e => e.ObjectId).ToArray()));
                // Уничтожить парсеры
                foreach (RecordLayerWrapper rlp in layersList)
                    rlp.BoundLayer.Dispose();
                // Завершить транзакцию и вывести список необработанных слоёв в консоль
                transaction.Commit();
                Workstation.Logger?.LogDebug("{ProcessingObject}: Транзакция завершена: Вывод сообщений о необработанных слоях:", nameof(LegendAssembler));
                Workstation.Logger?.LogInformation("{WrongLayersMessage}", wrongLayersStringBuilder.ToString());
                Workstation.Logger?.LogDebug("{ProcessingObject}: Команда выполнена.", nameof(LegendAssembler));
            }
        }

        /// <summary>
        /// Выбор всех слоёв в чертеже
        /// </summary>
        /// <returns>Слои чертежа</returns>
        private static IEnumerable<LayerTableRecord> GetAllLayerTableRecords()
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            LayerTable? layertable = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
            var layers = from ObjectId elem in layertable!
                         let ltr = transaction.GetObject(elem, OpenMode.ForWrite, false) as LayerTableRecord
                         where ltr.Name.StartsWith(NameParser.Current.Prefix + NameParser.Current.Separator)
                         select ltr;
            return layers;
        }

        /// <summary>
        /// Выбор всех слоёв объектов внутри заданной пользователем полилинии
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private static IEnumerable<LayerTableRecord> GetLayerTableRecordsByBounds()
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;

            // Выбор полилинии
            PromptEntityOptions peo = new("Выберите замкнутую полилинию")
            {
                AllowNone = false,
            };
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
                throw new System.Exception("Границы не выбраны");
            Polyline? boundingPolyline = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Polyline;
            if (!boundingPolyline!.Closed)
                throw new System.Exception("Неверная граница");

            // Преобразование полилинии в коллекцию точек
            Point3dCollection points = new();
            for (int i = 0; i < boundingPolyline.NumberOfVertices; i++)
                points.Add(boundingPolyline.GetPoint3dAt(i).TransformBy(Workstation.Editor.CurrentUserCoordinateSystem));

            // Создание фильтра слоёв, выбирающего объекты только со стандартным префиксом
            TypedValue[] tValues = new TypedValue[1];
            tValues.SetValue(new TypedValue((int)DxfCode.LayerName, $"{NameParser.Current.Prefix}{NameParser.Current.Separator}*"), 0);
            SelectionFilter selectionFilter = new(tValues);

            // Выбор объектов в полигоне
            PromptSelectionResult psResult = Workstation.Editor.SelectCrossingPolygon(points, selectionFilter);
            if (psResult.Status != PromptStatus.OK)
                throw new System.Exception("Не выбраны объекты в полигоне");
            SelectionSet selectionSet = psResult.Value;

            // Выбор объектов по набору и слоёв по объектам
            List<Entity> entities = selectionSet.GetObjectIds()
                                                .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                                                .ToList();
            return entities.Select(e => (LayerTableRecord)transaction.GetObject(e.LayerId, OpenMode.ForWrite))
                           .Distinct();
        }
    }
}






