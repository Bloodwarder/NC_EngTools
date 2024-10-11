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
            PromptSelectionResult result = Workstation.Editor.SelectImplied();

            if (result.Status == PromptStatus.OK)
            {
                SelectionSet selectionSet = result.Value;
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
                new CurrentLayerWrapper();
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
                    EntityLayerWrapper entlp = new EntityLayerWrapper(entity);
                }
                catch (WrongLayerException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    Workstation.Editor.WriteMessage(ex.Message);
                }
            }
        }

        private static void ProcessBulk(SelectionSet selectionSet)
        {
            Dictionary<string, EntityLayerWrapper> dct = new Dictionary<string, EntityLayerWrapper>();
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
                        dct.Add(entity.Layer, new EntityLayerWrapper(entity));
                    }
                    catch (WrongLayerException)
                    {
                        continue;
                    }
                }
            }
        }
    }

}






