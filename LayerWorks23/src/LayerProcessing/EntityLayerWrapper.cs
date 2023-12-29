using System.Collections.Generic;
using Teigha.DatabaseServices;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LayerWorks23.LayerProcessing;

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
            LayerProps lp = LayerPropertiesDictionary.GetValue(LayerInfo.Name, out _);
            foreach (Entity ent in BoundEntities)
            {
                ent.Layer = LayerInfo.Name;
                if (ent is Polyline pl)
                {
                    pl.LinetypeScale = lp.LTScale;
                    pl.ConstantWidth = lp.ConstantWidth;
                }
            }
        }
    }
}


