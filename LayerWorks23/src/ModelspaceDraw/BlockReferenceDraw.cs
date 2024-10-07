//System

//Modules
using HostMgd.ApplicationServices;
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using NanocadUtilities;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Отрисовывает вхождение блока
    /// </summary>
    public class BlockReferenceDraw : LegendObjectDraw
    {
        private string BlockName => LegendDrawTemplate!.BlockName ?? throw new NoPropertiesException("Нет наименования блока в шаблоне");
        private string FilePath => LegendDrawTemplate!.BlockPath ?? throw new NoPropertiesException("Нет пути к файлу с блоком в шаблоне");

        // Списки файлов и блоков для единоразовой обработки
        internal static HashSet<string> QueuedFiles = new HashSet<string>();
        internal static Dictionary<string, HashSet<string>> QueuedBlocks = new Dictionary<string, HashSet<string>>();
        // Проверка, выполнен ли иморт блоков
        private static bool _blocksImported = false;
        private static Dictionary<Document, DBObjectWrapper<BlockTable>> _blockTables = new Dictionary<Document, DBObjectWrapper<BlockTable>>();
        private static BlockTable BoundBlockTable
        {
            get
            {
                try
                {
                    BlockTable bt = _blockTables[Workstation.Document].Get();
                    if (!bt.IsWriteEnabled)
                    {
                        throw new System.Exception("Не открыта для чтения");
                    }
                    // TODO: исполнить более правильно - через DBObjectWrapper например
                    return bt;
                }
                catch
                {
                    DBObjectWrapper<BlockTable> wrapper = new DBObjectWrapper<BlockTable>(Workstation.Database.BlockTableId, OpenMode.ForWrite);
                    _blockTables[Workstation.Document] = wrapper;
                    return wrapper.Get();
                }
            }
            set
            {
                _blockTables[Workstation.Document] = new DBObjectWrapper<BlockTable>(value, OpenMode.ForWrite);
            }
        }

        static BlockReferenceDraw()
        {
            BoundBlockTable = (BlockTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite);
        }
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>

        public BlockReferenceDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer)
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        public BlockReferenceDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            // Перед отрисовкой первого объекта импортируем все блоки в очереди
            if (!_blocksImported)
            {
                ImportRecords(out HashSet<string> failedImports);
                foreach (string str in failedImports)
                    Workstation.Editor.WriteMessage($"Не удалось импортировать блок {str}");
                _blocksImported = true; // BUG: При ошибке в верхней транзакции значение остаётся true, а блоки не импортируются
            }
            // Отрисовываем объект
            ObjectId btrId = BoundBlockTable[BlockName];
            BlockReference bref = new BlockReference(new Point3d(Basepoint.X + LegendDrawTemplate!.BlockXOffset, Basepoint.Y + LegendDrawTemplate.BlockYOffset, 0d), btrId)
            {
                Layer = Layer.BoundLayer.Name
            };
            EntitiesList.Add(bref);
        }

        private void QueueImportBlockTableRecord(object? sender, EventArgs e)
        {
            if (BoundBlockTable.Has(BlockName))
                return;

            if (QueuedFiles.Count == 0)
            {
                // Обновляем таблицу блоков (по открытому чертежу)
                BoundBlockTable = (BlockTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite);
                // Сбрасывем переменную, показывающую, что импорт для данной задачи выполнен
                _blocksImported = false;
            }

            // Заполняем очереди блоков и файлов для импорта
            QueuedFiles.Add(LegendDrawTemplate!.BlockPath!);
            bool success = QueuedBlocks.TryGetValue(FilePath, out HashSet<string>? blocksQueue);
            if (!success)
                QueuedBlocks.Add(FilePath, blocksQueue = new HashSet<string>());
            blocksQueue!.Add(BlockName);
        }

        private static void ImportRecords(out HashSet<string> failedBlockImports)
        {
            failedBlockImports = new HashSet<string>();


            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                // По одному разу открываем базу данных каждого файла с блоками для условных
                foreach (string file in QueuedFiles)
                {
                    using (Database importDatabase = new Database(false, true))
                    {
                        try
                        {
                            importDatabase.ReadDwgFile(file, FileOpenMode.OpenForReadAndAllShare, false, null);
                            // Ищем все нужные нам блоки
                            BlockTable? importBlockTable = transaction.GetObject(importDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                            var importedBlocks = (from ObjectId blockId in importBlockTable!
                                                  let block = transaction.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord
                                                  where QueuedBlocks[file].Contains(block.Name)
                                                  select block).ToList();
                            // Заполняем сет с ненайденными блоками
                            foreach (BlockTableRecord block in importedBlocks)
                            {
                                if (!QueuedBlocks[file].Contains(block.Name))
                                    failedBlockImports.Add(block.Name);
                            }
                            // Добавляем все найденные блоки в таблицу блоков текущего чертежа
                            importBlockTable!.Database.WblockCloneObjects(new ObjectIdCollection(importedBlocks.Select(b => b.ObjectId).ToArray()), BoundBlockTable.ObjectId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                        }
                        catch (Exception)
                        {
                            Workstation.Editor.WriteMessage($"\nОшибка импорта блоков из файла \"{file}\"\n");
                            transaction.Abort();
                        }
                        finally
                        {
                            // Убираем файл из очереди
                            QueuedBlocks.Remove(file);
                        }
                    }
                }
            }
            QueuedFiles.Clear();
        }
    }
}
