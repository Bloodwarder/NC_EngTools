//System
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Teigha.Colors;

//internal modules
using LayerProcessing;
using ExternalData;
using Dictionaries;
using Teigha.Geometry;

using Legend;
using Loader.CoreUtilities;

namespace LayerWorks
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayersCommands
    {
        internal static string PrevStatus = "Сущ";
        internal static string PrevExtProject = "";
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        [CommandMethod("КАЛЬКА")]
        public void TransparentOverlayToggle()
        {
            Workstation.Define();
            Database db = Workstation.Database;
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;

            string tgtlayer = LayerParser.StandartPrefix + "_Калька";

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite, false) as LayerTable;
                if (!lt.Has(tgtlayer))
                {

                    LayerTableRecord ltrec = new LayerTableRecord
                    {
                        Name = tgtlayer,
                        Color = Color.FromRgb(255, 255, 255),
                        Transparency = new Transparency(166)
                    };
                    lt.Add(ltrec);
                    transaction.AddNewlyCreatedDBObject(ltrec, true);
                }
                else
                {
                    LayerTableRecord ltrec = (from ObjectId elem in lt
                                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForWrite, false)
                                              where ltr.Name == tgtlayer
                                              select ltr)
                                              .FirstOrDefault();
                    if (ltrec.IsFrozen || ltrec.IsOff)
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

        /// <summary>
        /// Изменение статуса объекта в соответствии с данными LayerParser
        /// </summary>
        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public void LayerStatusChange()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;


            PromptKeywordOptions pko = new PromptKeywordOptions($"Укажите статус объекта <{PrevStatus}> [Сущ/Демонтаж/Проект/Неутв/Неутв_демонтаж/Неутв_реорганизация]", "Сущ Демонтаж Проект Неутв Неутв_демонтаж Неутв_реорганизация")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult res = ed.GetKeywords(pko);
            if (res.Status == PromptStatus.OK) { PrevStatus = res.StringResult; }
            StatusTextDictionary.StTxtDictionary.TryGetValue(res.StringResult, out int val);
            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.StatusSwitch((Status)val);
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public void LayerAlter()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Alter();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою приписки, обозначающей переустройство
        /// </summary>
        [CommandMethod("ПЕРЕУСТРОЙСТВО", CommandFlags.Redraw)]
        public void LayerReconstruction()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.ReconstrSwitch();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою имени внешнего проекта (неутверждаемого)
        /// </summary>
        [CommandMethod("ВНЕШПРОЕКТ", CommandFlags.Redraw)]
        public void ExtAssign()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            PromptStringOptions pso = new PromptStringOptions($"Введите имя проекта, согласно которому отображён выбранный объект")
            {
                AllowSpaces = true,
                DefaultValue = PrevExtProject,
                UseDefaultValue = true,
            };
            PromptResult result = editor.GetString(pso);
            string extProjectName;
            if (result.Status != (PromptStatus.Error | PromptStatus.Cancel))
            {
                extProjectName = result.StringResult;
                PrevExtProject = extProjectName;
            }
            else
            {
                return;
            }

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.ExtProjNameAssign(extProjectName);
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Приведение свойств объекта или текущих переменных чертежа к стандарту (ширина и масштаб типов линий)
        /// </summary>
        [CommandMethod("СВС", CommandFlags.Redraw)]
        public void StandartLayerValues()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
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
            Editor ed = Workstation.Editor;
            PromptStringOptions pso = new PromptStringOptions($"Введите новый префикс обрабатываемых слоёв <{LayerParser.StandartPrefix}>")
            {
                AllowSpaces = false
            };
            string newprefix = ed.GetString(pso).StringResult;
            if (!string.IsNullOrEmpty(newprefix))
            {
                LayerParser.StandartPrefix = newprefix;
            }

        }
    }

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
                             where ltr.Name.StartsWith(LayerParser.StandartPrefix + "_")
                             select ltr;
                int errorCount = 0;
                int successCount = 0;
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        new ChapterStoreLayerParser(ltr);
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

                var layerchapters = ChapterStoredLayerParsers.StoredLayerStates[doc].Where(l => l.EngType != null).Select(l => l.EngType).Distinct().OrderBy(l => l).ToList();
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
                    ChapterStoredLayerParsers.Reset();
                    if (ActiveChapterState != null)
                    {
                        LayerChecker.LayerAddedEvent -= NewLayerHighlight;
                        ActiveChapterState[doc] = null;
                    }

                }
                else
                {
                    ActiveChapterState[doc] = result.StringResult;
                    ChapterStoredLayerParsers.Highlight(ActiveChapterState[doc]);
                    LayerChecker.LayerAddedEvent += NewLayerHighlight;
                }
                transaction.Commit();
            }
        }

        internal void NewLayerHighlight(object sender, System.EventArgs e)
        {
            Document doc = Workstation.Document;
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                ChapterStoreLayerParser cslp = new ChapterStoreLayerParser((LayerTableRecord)sender);
                cslp.Push(ActiveChapterState[doc]);
                transaction.Commit();
            }

        }
    }


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
            LayerChecker.Check(string.Concat(LayerParser.StandartPrefix, "_Условные"));

            //получить точку вставки
            PromptPointOptions ppo = new PromptPointOptions("Укажите точку вставки")
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
                StringBuilder wrongLayersStringBuilder = new StringBuilder();
                List<RecordLayerParser> layersList = new List<RecordLayerParser>();
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        RecordLayerParser rlp = new RecordLayerParser(ltr);
                        if (!LayerLegendDictionary.CheckKey(rlp.MainName))
                        {
                            wrongLayersStringBuilder.AppendLine($"Нет данных для слоя {string.Concat(LayerParser.StandartPrefix, "_", rlp.MainName)}");
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
                foreach (RecordLayerParser rlp in layersList)
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
                foreach (RecordLayerParser rlp in layersList)
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
                         where ltr.Name.StartsWith(LayerParser.StandartPrefix + "_")
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
            tValues.SetValue(new TypedValue((int)DxfCode.LayerName, $"{LayerParser.StandartPrefix}_*"), 0);
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


    internal static class LayerChanger
    {
        internal static int MaxSimple { get; set; } = 5;

        internal static void UpdateActiveLayerParsers()
        {
            PromptSelectionResult result = Workstation.Editor.SelectImplied();

            if (result.Status == PromptStatus.OK)
            {
                SelectionSet selectionSet = result.Value;
                if (selectionSet.Count < MaxSimple)
                {
                    ChangerSimple(selectionSet);
                }
                else
                {
                    ChangerBig(selectionSet);
                }
            }
            else
            {
                new CurrentLayerParser();
            }
        }

        private static void ChangerSimple(SelectionSet selectionSet)
        {

            foreach (Entity entity in (from ObjectId elem in selectionSet.GetObjectIds()
                                       let ent = Workstation.TransactionManager.TopTransaction.GetObject(elem, OpenMode.ForWrite) as Entity
                                       select ent).ToArray())
            {
                try
                {
                    EntityLayerParser entlp = new EntityLayerParser(entity);
                }
                catch (WrongLayerException)
                {
                    continue;
                }
            }
        }

        private static void ChangerBig(SelectionSet selectionSet)
        {
            Dictionary<string, EntityLayerParser> dct = new Dictionary<string, EntityLayerParser>();
            foreach (var entity in (from ObjectId elem in selectionSet.GetObjectIds()
                                    let ent = Workstation.TransactionManager.TopTransaction.GetObject(elem, OpenMode.ForWrite) as Entity
                                    select ent).ToArray())
            {
                if (dct.ContainsKey(entity.Layer))
                {
                    dct[entity.Layer].BoundEntities.Add(entity);
                }
                else
                {
                    try
                    {
                        dct.Add(entity.Layer, new EntityLayerParser(entity));
                    }
                    catch (WrongLayerException)
                    {
                        continue;
                    }
                }
            }
        }
    }

    static class LayerChecker
    {
        internal static event System.EventHandler LayerAddedEvent;
        internal static void Check(string layername)
        {

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt.Has(layername))
                    {
                        LayerProps lp = LayerPropertiesDictionary.GetValue(layername, out bool propsgetsuccess);
                        LayerTableRecord ltRecord = AddLayer(layername, lp);

                        //Process new layer if isolated chapter visualization is active
                        System.EventArgs e = new System.EventArgs();
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, e);

                    }
                    else
                    {
                        return;
                    }
                }
                catch (NoPropertiesException)
                {
                    throw new NoPropertiesException("Проверка слоя не удалась");
                }
            }
        }

        internal static void Check(LayerParser layer)
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable lt = transaction.GetObject(Workstation.Database.LayerTableId, OpenMode.ForRead, false) as LayerTable;
                    if (!lt.Has(layer.OutputLayerName))
                    {
                        LayerProps lp = LayerPropertiesDictionary.GetValue(layer, out bool propsgetsuccess);
                        LayerTableRecord ltRecord = AddLayer(layer.OutputLayerName, lp);

                        //Process new layer if isolated chapter visualization is active
                        System.EventArgs e = new System.EventArgs();
                        transaction.Commit();
                        LayerAddedEvent?.Invoke(ltRecord, e);

                    }
                    else
                    {
                        return;
                    }
                }
                catch (NoPropertiesException)
                {
                    throw new NoPropertiesException("Проверка слоя не удалась");
                }
            }
        }

        private static LayerTableRecord AddLayer(string layername, LayerProps lp)
        {
            Teigha.DatabaseServices.TransactionManager manager = Workstation.TransactionManager;
            Transaction transaction = manager.TopTransaction;
            Database database = Workstation.Database;
            ObjectId linetypeRecordId = FindLinetype(lp.LTName, out bool ltgetsuccess);
            if (!ltgetsuccess)
            {
                string str = $"Не найден тип линий для слоя {layername}. Назначен тип линий Continious";
                Workstation.Editor.WriteMessage(str);
            }
            LayerTableRecord ltRecord = new LayerTableRecord
            {
                Name = layername,
                Color = Color.FromRgb(lp.Red, lp.Green, lp.Blue),
                LineWeight = (LineWeight)lp.LineWeight,
                LinetypeObjectId = linetypeRecordId
                //Transparency = new Teigha.Colors.Transparency(Teigha.Colors.TransparencyMethod.ByAlpha)
            };
            LayerTable layerTable = transaction.GetObject(database.LayerTableId, OpenMode.ForWrite) as LayerTable;
            layerTable.Add(ltRecord);
            transaction.AddNewlyCreatedDBObject(ltRecord, true);
            return ltRecord;
        }

        internal static ObjectId FindLinetype(string linetypename, out bool ltgetsuccess)
        {
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;
            LinetypeTable ltt = tm.TopTransaction.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false) as LinetypeTable;
            ltgetsuccess = true;
            if (!ltt.Has(linetypename))
            {
                FileInfo fi = new FileInfo(PathOrganizer.GetPath("Linetypes"));
                try
                {
                    db.LoadLineTypeFile(linetypename, fi.FullName);
                }
                catch
                {
                    ltgetsuccess = false;
                    return SymbolUtilityServices.GetLinetypeContinuousId(db);
                }
            }
            return ltt[linetypename];
        }

    }

}






