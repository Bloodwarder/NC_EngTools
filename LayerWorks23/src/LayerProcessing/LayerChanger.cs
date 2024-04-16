//nanoCAD
using HostMgd.EditorInput;
using Teigha.DatabaseServices;

//internal modules
using NanocadUtilities;
using NameClassifiers;
using LayerWorks.LayerProcessing;

namespace LayerWorks23.LayerProcessing
{
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
                new CurrentLayerWrapper();
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
                    EntityLayerWrapper entlp = new EntityLayerWrapper(entity);
                }
                catch (WrongLayerException)
                {
                    continue;
                }
            }
        }

        private static void ChangerBig(SelectionSet selectionSet)
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






