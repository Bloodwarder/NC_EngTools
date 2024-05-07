using NameClassifiers;
using Teigha.DatabaseServices;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks.LayerProcessing;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом чертежа (Entity)
    /// </summary>
    public class EntityLayerWrapper : LayerWrapper
    {
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
            BoundEntities.Add(entity); ActiveLayerWrappers.Add(this);
        }
        /// <summary>
        /// Коллекция связанных объектов чертежа
        /// </summary>
        public List<Entity> BoundEntities = new List<Entity>();
        /// <summary>
        /// Назначение выходного слоя и соответствующих ему свойств связанным объектам чертежа
        /// </summary>
        public override void Push()
        {
            LayerChecker.Check(this);
            bool success = LayerPropertiesDictionary.TryGetValue(LayerInfo.Name, out LayerProps? lp);
            foreach (Entity ent in BoundEntities)
            {
                ent.Layer = LayerInfo.Name;
                if (ent is Polyline pl && success)
                {
                    pl.LinetypeScale = lp!.LTScale;
                    pl.ConstantWidth = lp!.ConstantWidth;
                }
            }
        }
    }
}


