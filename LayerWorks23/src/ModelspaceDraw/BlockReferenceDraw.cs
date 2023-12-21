//System
using System;
using System.Linq;
using System.Collections.Generic;

//Modules
using LoaderCore.Utilities;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Geometry;
using HostMgd.ApplicationServices;
using LayerWorks.LayerProcessing;
using LayersIO.DataTransfer;

namespace LayerWorks.ModelspaceDraw
{
    /// <summary>
    /// Отрисовывает вхождение блока
    /// </summary>
    public class BlockReferenceDraw : LegendObjectDraw
    {
        private string BlockName => LegendDrawTemplate.BlockName;
        private string FilePath => LegendDrawTemplate.BlockPath;

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
                    return _blockTables[Workstation.Document].Get();
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
            BoundBlockTable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;
        }
        /// <summary>
        /// Конструктор класса без параметров. После вызова задайте базовую точку и шаблон данных отрисовки LegendDrawTemplate
        /// </summary>
        public BlockReferenceDraw()
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer = null) : base(basepoint, layer)
        {
            TemplateSetEventHandler += QueueImportBlockTableRecord;
        }
        internal BlockReferenceDraw(Point2d basepoint, RecordLayerParser layer, LegendDrawTemplate template) : base(basepoint, layer)
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
                _blocksImported = true;
            }
            // Отрисовываем объект
            ObjectId btrId = BoundBlockTable[BlockName];
            BlockReference bref = new BlockReference(new Point3d(Basepoint.X + LegendDrawTemplate.BlockXOffset, Basepoint.Y + LegendDrawTemplate.BlockYOffset, 0d), btrId)
            {
                Layer = Layer.BoundLayer.Name
            };
            EntitiesList.Add(bref);
        }

        private void QueueImportBlockTableRecord(object sender, EventArgs e)
        {
            if (BoundBlockTable.Has(BlockName))
                return;

            if (QueuedFiles.Count == 0)
            {
                // Обновляем таблицу блоков (по открытому чертежу)
                BoundBlockTable = Workstation.TransactionManager.TopTransaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;
                // Сбрасывем переменную, показывающую, что импорт для данной задачи выполнен
                _blocksImported = false;
            }

            // Заполняем очереди блоков и файлов для импорта
            QueuedFiles.Add(LegendDrawTemplate.BlockPath);
            HashSet<string> blocksQueue;
            bool success = QueuedBlocks.TryGetValue(FilePath, out blocksQueue);
            if (!success)
                QueuedBlocks.Add(FilePath, blocksQueue = new HashSet<string>());
            blocksQueue.Add(BlockName);
        }

        private static void ImportRecords(out HashSet<string> failedBlockImports)
        {
            failedBlockImports = new HashSet<string>();
            Transaction transaction = Workstation.TransactionManager.TopTransaction;
            // По одному разу открываем базу данных каждого файла с блоками для условных
            foreach (string file in QueuedFiles)
            {
                using (Database importDatabase = new Database(false, true))
                {
                    try
                    {
                        importDatabase.ReadDwgFile(file, FileOpenMode.OpenForReadAndAllShare, false, null);
                        // Ищем все нужные нам блоки
                        BlockTable importBlockTable = transaction.GetObject(importDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;
                        var importedBlocks = (from ObjectId blockId in importBlockTable
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
                        importBlockTable.Database.WblockCloneObjects(new ObjectIdCollection(importedBlocks.Select(b => b.ObjectId).ToArray()), BoundBlockTable.ObjectId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                    }
                    catch (Exception)
                    {
                        Workstation.Editor.WriteMessage($"\nОшибка импорта блоков из файла \"{file}\"\n");
                    }
                    finally
                    {
                        // Убираем файл из очереди
                        QueuedBlocks.Remove(file);
                    }
                }
            }
            QueuedFiles.Clear();
        }
    }
}
