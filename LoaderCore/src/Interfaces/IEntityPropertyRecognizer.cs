using System.Collections.Generic;

namespace LoaderCore.Interfaces
{
    public interface IEntityPropertyRecognizer<TEntity, TProperty> where TEntity : class
    {
        public TProperty RecognizeProperty(TEntity entity);
        public Dictionary<TEntity, TProperty> RecognizeProperty(IEnumerable<TEntity> entities);

    }
}
