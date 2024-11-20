using Microsoft.Extensions.Logging;
//nanoCAD
using HostMgd.EditorInput;
using NameClassifiers;
//internal modules
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    internal static class SelectionHandler
    {
        internal static int MaxSimple { get; set; } = 5;

        internal static void UpdateActiveLayerWrappers()
        {
            Workstation.Logger?.LogDebug("{ProcessingObject}: Получение выбранных объектов", nameof(SelectionHandler));
            PromptSelectionResult result = Workstation.Editor.SelectImplied();

            if (result.Status == PromptStatus.OK)
            {

                SelectionSet selectionSet = result.Value;
                Workstation.Logger?.LogDebug("{ProcessingObject}: Выбрано {Count} объектов", nameof(SelectionHandler), selectionSet.Count);

                if (selectionSet.Count < MaxSimple)
                {
                    ProcessSimple(selectionSet);
                }
                else
                {
                    ProcessBulk(selectionSet);
                }
            }
            else
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Объекты не выбраны. Применение к текущему слою", nameof(SelectionHandler));
                _ = new CurrentLayerWrapper();
            }
        }

        private static void ProcessSimple(SelectionSet selectionSet)
        {

            foreach (Entity entity in (from ObjectId elem in selectionSet.GetObjectIds()
                                       let ent = Workstation.TransactionManager.TopTransaction.GetObject(elem, OpenMode.ForWrite) as Entity
                                       select ent).ToArray())
            {
                try
                {
                    EntityLayerWrapper entlp = new(entity);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Создан EntityLayerWrapper для объекта {ObjectType} слоя {LayerName}",
                                                 nameof(SelectionHandler),
                                                 entity.GetType().Name,
                                                 entity.Layer);
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Logger?.LogDebug(
                        "{ProcessingObject}: Объект {ObjectType} слоя {LayerName} не обработан: {Exception}",
                        nameof(SelectionHandler),
                        entity.GetType().Name,
                        entity.Layer,
                        ex.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    Workstation.Logger?.LogWarning("Ошибка: {Exception}", ex.Message);
                }
            }
        }

        private static void ProcessBulk(SelectionSet selectionSet)
        {
            Dictionary<string, EntityLayerWrapper> dct = new();
            foreach (var entity in (from ObjectId elem in selectionSet.GetObjectIds()
                                    let ent = Workstation.TransactionManager.TopTransaction.GetObject(elem, OpenMode.ForWrite) as Entity
                                    select ent).ToArray())
            {
                if (dct.ContainsKey(entity.Layer))
                {
                    dct[entity.Layer].BoundEntities.Add(entity);
                    Workstation.Logger?.LogDebug("{ProcessingObject}: Объект {ObjectType} слоя {LayerName} добавлен к существующему EntityLayerWrapper", nameof(SelectionHandler), entity.GetType().Name, entity.Layer);
                }
                else
                {
                    try
                    {
                        dct.Add(entity.Layer, new EntityLayerWrapper(entity));
                        Workstation.Logger?.LogDebug("{ProcessingObject}: Создан EntityLayerWrapper для объекта {ObjectType} слоя {LayerName}", nameof(SelectionHandler), entity.GetType().Name, entity.Layer);
                    }
                    catch (WrongLayerException ex)
                    {
                        Workstation.Logger?.LogDebug("{ProcessingObject}: Объект {ObjectType} слоя {LayerName} не обработан: {Exception}", nameof(SelectionHandler), entity.GetType().Name, entity.Layer, ex.Message);
                        continue;
                    }
                }
            }
        }
    }

}






