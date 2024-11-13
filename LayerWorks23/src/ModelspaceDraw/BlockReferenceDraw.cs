//System

//Modules
using HostMgd.ApplicationServices;
using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;
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
        private static bool _blocksImported = false;


        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        static BlockReferenceDraw()
        {
            BoundBlockTable = (BlockTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite);
        }

        public BlockReferenceDraw(Point2d basepoint, RecordLayerWrapper layer) : base(basepoint, layer)
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        public BlockReferenceDraw(Point2d basepoint, RecordLayerWrapper layer, LegendDrawTemplate template) : base(basepoint, layer)
        {
            LegendDrawTemplate = template;
            QueueImportBlockTableRecord();
        }


        // Списки файлов и блоков для единоразовой обработки
        internal static HashSet<string> QueuedFiles { get; set; } = new HashSet<string>();
        internal static Dictionary<string, HashSet<string>> QueuedBlocks { get; set; } = new Dictionary<string, HashSet<string>>();
        // Проверка, выполнен ли иморт блоков
        private static Dictionary<Document, DBObjectWrapper<BlockTable>> _blockTables = new Dictionary<Document, DBObjectWrapper<BlockTable>>();
        private static BlockTable BoundBlockTable
        {
            get
            {
                try
                {
                    BlockTable bt = _blockTables[Workstation.Document].Get();
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
        private string BlockName => LegendDrawTemplate!.BlockName ?? throw new NoPropertiesException("Нет наименования блока в шаблоне");
        private string FilePath => LegendDrawTemplate!.BlockPath ?? throw new NoPropertiesException("Нет пути к файлу с блоком в шаблоне");


        /// <inheritdoc/>
        protected override void CreateEntities()
        {
            // Перед отрисовкой первого объекта импортируем все блоки в очереди
            if (!_blocksImported)
            {
                ImportRecords(out HashSet<string> failedImports);
                foreach (string str in failedImports)
                    Workstation.Logger?.LogWarning("Не удалось импортировать блок {Block}", str);
                _blocksImported = true;
            }
            // Отрисовываем объект
            ObjectId btrId = BoundBlockTable[BlockName];
            double x = Basepoint.X + LegendDrawTemplate!.BlockXOffset;
            double y = Basepoint.Y + LegendDrawTemplate.BlockYOffset;
            BlockReference bref = new(new Point3d(x, y, 0d), btrId)
            {
                Layer = Layer.BoundLayer.Name
            };
            EntitiesList.Add(bref);
            //Workstation.Logger?.LogDebug("{ProcessingObject}: Объект слоя {Layer} добавлен в список для отрисовки", nameof(BlockReferenceDraw), bref.Layer);
        }

        private void QueueImportBlockTableRecord(object? sender, EventArgs e) => QueueImportBlockTableRecord();

        private void QueueImportBlockTableRecord()
        {
            if (BoundBlockTable.Has(BlockName))
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Блок {Block} присутствует в таблице чертежа. Пропуск постановки в очередь", nameof(BlockReferenceDraw), BlockName);
                return;
            }
            Workstation.Logger?.LogDebug("{ProcessingObject}: Постановка блока {Block} в очередь импорта", nameof(BlockReferenceDraw), BlockName);


            if (QueuedFiles.Count == 0)
            {
                // Обновляем таблицу блоков (по открытому чертежу)
                BoundBlockTable = (BlockTable)Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite);
                // Сбрасывем переменную, показывающую, что импорт для данной задачи выполнен
                _blocksImported = false;
            }

            // Заполняем очереди блоков и файлов для импорта
            Workstation.Logger?.LogDebug("{ProcessingObject}: Файл для импорта блока - {File}", nameof(BlockReferenceDraw), LegendDrawTemplate!.BlockPath!);
            QueuedFiles.Add(LegendDrawTemplate!.BlockPath!);
            bool success = QueuedBlocks.TryGetValue(FilePath, out HashSet<string>? blocksQueue);
            if (!success)
                QueuedBlocks.Add(FilePath, blocksQueue = new HashSet<string>());
            blocksQueue!.Add(BlockName);
            Workstation.Logger?.LogDebug("{ProcessingObject}: Блок {Block} поставлен в очередь импорта", nameof(BlockReferenceDraw), BlockName);
        }

        private static void ImportRecords(out HashSet<string> failedBlockImports)
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало импорта блоков", nameof(BlockReferenceDraw));

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
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Открытие файла {File}", nameof(BlockReferenceDraw), file);
                            importDatabase.ReadDwgFile(file, FileOpenMode.OpenForReadAndAllShare, false, null);
                            // Ищем все нужные нам блоки
                            BlockTable? importBlockTable = transaction.GetObject(importDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                            var importedBlocks = (from ObjectId blockId in importBlockTable!
                                                  let block = transaction.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord
                                                  where QueuedBlocks[file].Contains(block.Name) // TODO: НЕ ОБРАБАТЫВАЕТ ДИНАМИЧЕСКИЕ БЛОКИ. Исправить позже, а пока не использовать в файле шаблона
                                                  select block).ToList();
                            
                            string blockNames = string.Join(", ", importedBlocks.Select(b => b.Name));
                            // Заполняем сет с ненайденными блоками
                            foreach (BlockTableRecord block in importedBlocks)
                            {
                                if (!QueuedBlocks[file].Contains(block.Name))
                                {
                                    Workstation.Logger?.LogDebug("{ProcessingObject}: Не найден блок {Block}", nameof(BlockReferenceDraw), block.Name);
                                    failedBlockImports.Add(block.Name);
                                }
                            }
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Выбраны блоки для копирования: {Blocks}", nameof(BlockReferenceDraw), blockNames);
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Импорт блоков", nameof(BlockReferenceDraw));
                            // Добавляем все найденные блоки в таблицу блоков текущего чертежа
                            importBlockTable!.Database.WblockCloneObjects(new ObjectIdCollection(importedBlocks.Select(b => b.ObjectId).ToArray()),
                                                                          BoundBlockTable.ObjectId,
                                                                          new IdMapping(),
                                                                          DuplicateRecordCloning.Ignore,
                                                                          false);

                            transaction.Commit();
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Транзакция завершена", nameof(BlockReferenceDraw));
                        }
                        catch (Exception ex)
                        {
                            Workstation.Logger?.LogWarning(ex, "Ошибка импорта блоков из файла \"{File}\": {Message}", file, ex.Message);
                            transaction.Abort();
                        }
                        finally
                        {
                            // Убираем файл из очереди
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Удаление файла {File} из очереди обработки", nameof(BlockReferenceDraw), file);
                            QueuedBlocks.Remove(file);
                        }
                    }
                }
            }
            Workstation.Logger?.LogDebug("{ProcessingObject}: Очистка очереди файлов для импорта блоков", nameof(BlockReferenceDraw));
            QueuedFiles.Clear();
        }
    }
}
