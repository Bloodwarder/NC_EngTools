using HostMgd.EditorInput;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace Utilities
{
    internal static class EntitySelector
    {
        internal static bool TryGetEntity<T>(string message, out T? entity, string? blockName = null) where T : Entity
        {
            PromptEntityOptions peo = new(message)
            { AllowNone = false };
            peo.AddAllowedClass(typeof(T), true);
            peo.SetRejectMessage("Неверный тип объекта");
            PromptEntityResult result = Workstation.Editor.GetEntity(peo);
            if (result.Status != PromptStatus.OK)
            {
                entity = null;
                return false;
            }
            entity = (T)Workstation.TransactionManager.TopTransaction.GetObject(result.ObjectId, OpenMode.ForWrite);
            Highlighter.Highlight(entity);
            if (blockName != null)
            {
                if (entity is BlockReference blockReference && blockReference.BlockTableRecordName() != blockName)
                {
                    entity = null;
                    return false;
                }
            }
            return true;
        }
    }

}
