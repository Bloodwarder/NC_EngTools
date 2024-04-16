//System
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
//nanoCAD
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Teigha.Colors;

//internal modules
using Teigha.Geometry;
using LoaderCore.Utilities;
using LayerWorks.LayerProcessing;
using LayerWorks.Legend;
using LayersIO.ExternalData;
using LayerWorks23.LayerProcessing;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс для автосборки условных обозначений на основе слоёв чертежа и логики LayerParser
    /// </summary>
    public class LegendAssembler
    {
        private static readonly string[] _filterKeywords = { "Существующие", "Общая_таблица", "Внутренние", "Утв_и_неутв", "Разделённые" };
        private static readonly string[] _modeKeywords = { "Все", "Полигон" };

        /// <summary>
        /// Автосборка условных обозначений на основе слоёв чертежа и логики LayerParser
        /// </summary>
        [CommandMethod("АВТОСБОРКА")]
        public void Assemble()
        {
            Workstation.Define();
            LayerChecker.Check(string.Concat(LayerWrapper.StandartPrefix, "_Условные"));

            //получить точку вставки
            PromptPointOptions ppo = new("Укажите точку вставки")
            {
                UseBasePoint = false,
                AllowNone = false
            };
            PromptPointResult pointResult = Workstation.Editor.GetPoint(ppo);
            if (pointResult.Status != PromptStatus.OK)
                return;
            Point3d p3d = pointResult.Value;

            //получить таблицу слоёв и слои
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                Workstation.Database.Cecolor = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                // Выбрать режим поиска слоёв (весь чертёж или в границах полилинии)
                string layerFindMode = GetStringKeywordResult(_modeKeywords, "Выберите режим поиска слоёв:");
                int modeIndex = Array.IndexOf(_modeKeywords, layerFindMode);
                // Получить записи таблицы слоёв в зависимости от режима поиска
                List<LayerTableRecord> layers = new List<LayerTableRecord>();
                switch (modeIndex)
                {
                    case 0:
                        layers.AddRange(GetAllLayerTableRecords());
                        break;
                    case 1:
                        layers.AddRange(GetLayerTableRecordsByBounds());
                        break;
                }
                // Создать парсеры для слоёв
                StringBuilder wrongLayersStringBuilder = new();
                List<RecordLayerWrapper> layersList = new List<RecordLayerWrapper>();
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        RecordLayerWrapper rlp = new RecordLayerWrapper(ltr);
                        if (!LayerLegendDictionary.CheckKey(rlp.LayerInfo.MainName))
                        {
                            wrongLayersStringBuilder.AppendLine($"Нет данных для слоя {rlp.LayerInfo.Prefix}{rlp.LayerInfo.ParentParser.Separator}{rlp.LayerInfo.MainName}");
                            continue;
                        }
                        layersList.Add(rlp);
                    }
                    catch (WrongLayerException ex)
                    {
                        wrongLayersStringBuilder.AppendLine(ex.Message);
                        continue;
                    }
                }
                // Создать шаблон ячейки для каждого успешно обработанного слоя
                List<LegendGridCell> cells = new List<LegendGridCell>();
                foreach (RecordLayerWrapper rlp in layersList)
                {
                    cells.Add(new LegendGridCell(rlp));
                }
                // Выбрать фильтр (режим компоновки)
                string filterModeString = GetStringKeywordResult(_filterKeywords, "Выберите режим компоновки:");
                TableFilter filter = GetFilter(filterModeString);
                // Запустить компоновщик таблиц условных и получить созданные объекты чертежа для вставки в ModelSpace
                GridsComposer composer = new GridsComposer(cells, filter);
                composer.Compose(p3d);
                List<Entity> entitiesList = composer.DrawGrids();
                // Получить таблицу блоков и ModelSpace, затем вставить объекты таблиц условных в чертёж
                BlockTable blocktable = transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                BlockTableRecord modelspace = transaction.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                Workstation.Database.Cecolor = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                foreach (Entity e in entitiesList)
                {
                    modelspace.AppendEntity(e);
                    transaction.AddNewlyCreatedDBObject(e, true); // и в транзакцию
                }
                // Поднять наверх в таблице порядка прорисовки все созданные объекты кроме штриховок
                DrawOrderTable drawOrderTable = (DrawOrderTable)transaction.GetObject(modelspace.DrawOrderTableId, OpenMode.ForWrite);
                drawOrderTable.MoveToTop(new ObjectIdCollection(entitiesList.Where(e => !(e is Hatch)).Select(e => e.ObjectId).ToArray()));
                // Уничтожить парсеры
                foreach (RecordLayerWrapper rlp in layersList)
                    rlp.BoundLayer.Dispose();
                // Завершить транзакцию и вывести список необработанных слоёв в консоль
                transaction.Commit();
                Workstation.Editor.WriteMessage(wrongLayersStringBuilder.ToString());
            }
        }

        /// <summary>
        /// Выбор всех слоёв в чертеже
        /// </summary>
        /// <returns>Слои чертежа</returns>
        private IEnumerable<LayerTableRecord> GetAllLayerTableRecords()
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            LayerTable layertable = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
            var layers = from ObjectId elem in layertable
                         let ltr = transaction.GetObject(elem, OpenMode.ForWrite, false) as LayerTableRecord
                         where ltr.Name.StartsWith(LayerWrapper.StandartPrefix + NameParser.LoadedParsers[LayerWrapper.StandartPrefix].Separator)
                         select ltr;
            return layers;
        }

        /// <summary>
        /// Выбор всех слоёв объектов внутри заданной пользователем полилинии
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private IEnumerable<LayerTableRecord> GetLayerTableRecordsByBounds()
        {
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            // Выбор полилинии
            PromptEntityOptions peo = new PromptEntityOptions("Выберите замкнутую полилинию")
            {
                AllowNone = false,
            };
            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
                throw new System.Exception("Границы не выбраны");
            Entity boundingPolyline = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;
            if (!(boundingPolyline is Polyline pl && pl.Closed))
                throw new System.Exception("Неверная граница");
            // Преобразование полилинии в коллекцию точек
            Point3dCollection points = new Point3dCollection();
            for (int i = 0; i < pl.NumberOfVertices; i++)
                points.Add(pl.GetPoint3dAt(i).TransformBy(Workstation.Editor.CurrentUserCoordinateSystem));
            // Создание фильтра слоёв, выбирающего объекты только со стандартным префиксом
            TypedValue[] tValues = new TypedValue[1];
            tValues.SetValue(new TypedValue((int)DxfCode.LayerName, $"{LayerWrapper.StandartPrefix}{NameParser.LoadedParsers[LayerWrapper.StandartPrefix].Separator}*"), 0);
            SelectionFilter selectionFilter = new SelectionFilter(tValues);
            // Выбор объектов в полигоне
            PromptSelectionResult psResult = Workstation.Editor.SelectCrossingPolygon(points, selectionFilter);
            if (psResult.Status != PromptStatus.OK)
                throw new System.Exception("Не выбраны объекты в полигоне");
            SelectionSet selectionSet = psResult.Value;
            // Выбор объектов по набору и слоёв по объектам
            List<Entity> entities = selectionSet.GetObjectIds().Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead)).ToList();
            return entities.Select(e => transaction.GetObject(e.LayerId, OpenMode.ForWrite) as LayerTableRecord).Distinct();
        }

        private string GetStringKeywordResult(string[] keywordsArray, string message)
        {
            PromptKeywordOptions pko = new PromptKeywordOptions($"{message} [{string.Join("/", keywordsArray)}]", string.Join(" ", keywordsArray))
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult keywordResult = Workstation.Editor.GetKeywords(pko);
            if (keywordResult.Status != PromptStatus.OK)
                throw new System.Exception("Ошибка ввода");
            return keywordResult.StringResult;
        }

        private TableFilter GetFilter(string keyword)
        {
            return (TableFilter)Array.IndexOf(_filterKeywords, keyword);
        }
    }

}






