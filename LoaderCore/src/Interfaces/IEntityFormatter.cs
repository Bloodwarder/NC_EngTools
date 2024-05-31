
using Teigha.DatabaseServices;

namespace LoaderCore.Interfaces
{
    public interface IEntityFormatter
    {
        public void FormatEntity(Entity entity);
        public void FormatEntity(Entity entity, string key);
    }
}
