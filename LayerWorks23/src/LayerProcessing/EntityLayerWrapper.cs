using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом чертежа (Entity)
    /// </summary>
    public class EntityLayerWrapper : LayerWrapper
    {
        private static readonly IEntityFormatter _entityFormatter;

        static EntityLayerWrapper()
        {
            _entityFormatter = LoaderCore.NcetCore.ServiceProvider.GetService<IEntityFormatter>()!;
        }
        internal EntityLayerWrapper(string layername) : base(layername)
        {
            ActiveLayerWrappers.Add(this);
        }
        /// <summary>
        /// Конструктор, принимающий объект чертежа
        /// </summary>
        /// <param name="entity"></param>
        public EntityLayerWrapper(Entity entity) : base(entity.Layer)
        {
            BoundEntities.Add(entity); 
            ActiveLayerWrappers.Add(this);
        }
        /// <summary>
        /// Коллекция связанных объектов чертежа
        /// </summary>
        public List<Entity> BoundEntities = new();
        /// <summary>
        /// Назначение выходного слоя и соответствующих ему свойств связанным объектам чертежа
        /// </summary>
        public override void Push()
        {
            LayerChecker.Check(this);
            foreach (Entity ent in BoundEntities)
            {
                ent.Layer = LayerInfo.Name;
                _entityFormatter.FormatEntity(ent, LayerInfo.TrueName);
            }
        }
    }
}


