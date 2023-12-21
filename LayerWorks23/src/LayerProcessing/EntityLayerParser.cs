using System.Collections.Generic;
using Teigha.DatabaseServices;
using LayerWorks.Commands;
using LayersIO.DataTransfer;
using LayersIO.ExternalData;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом чертежа (Entity)
    /// </summary>
    public class EntityLayerParser : LayerParser
    {
        internal EntityLayerParser(string layername) : base(layername)
        {
            ActiveLayerParsers.Add(this);
        }
        /// <summary>
        /// Конструктор, принимающий объект чертежа
        /// </summary>
        /// <param name="entity"></param>
        public EntityLayerParser(Entity entity) : base(entity.Layer)
        {
            BoundEntities.Add(entity); ActiveLayerParsers.Add(this);
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
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out _);
            foreach (Entity ent in BoundEntities)
            {
                ent.Layer = OutputLayerName;
                if (ent is Polyline pl)
                {
                    pl.LinetypeScale = lp.LTScale;
                    pl.ConstantWidth = lp.ConstantWidth;
                }
            }
        }
    }
}


