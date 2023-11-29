//System
using System.Linq;
using System.Collections.Generic;
//nanoCAD
using HostMgd.EditorInput;
using Teigha.DatabaseServices;

//internal modules
using Loader.CoreUtilities;
using LayerWorks.LayerProcessing;

namespace LayerWorks.Commands
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

}






