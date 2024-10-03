using NanocadUtilities;
using Teigha.DatabaseServices;

namespace Utilities
{
    internal static class Highlighter
    {
        private readonly static List<DBObjectWrapper<Entity>> _list = new();


        internal static void Highlight(Entity entity)
        {
            _list.Add(new DBObjectWrapper<Entity>(entity, OpenMode.ForWrite));
            entity.Highlight();
        }

        internal static void Unhighlight()
        {
            try
            {
                foreach (DBObjectWrapper<Entity> entity in _list)
                    entity.Get().Unhighlight();
            }
            finally
            {
                _list.Clear();
            }
        }
    }

}
