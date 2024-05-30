
using Teigha.DatabaseServices;

namespace LoaderCore.Interfaces
{
    public interface IEntityFormatter
    {
        public void FormatEntity(Entity entity);
        public void FormatEntity<T>(T entity) where T : Entity;
    }
}
