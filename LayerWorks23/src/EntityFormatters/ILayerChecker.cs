using LayersIO.DataTransfer;
using LayerWorks.LayerProcessing;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public interface ILayerChecker
    {
        public ObjectId Check(LayerInfo layerInfo);

        /// <summary>
        /// Ищет в таблице слоёв слой с указанным именем и при его отсутствии добавляет его, 
        /// используя репозиторий с данными стандартных слоёв
        /// </summary>
        /// <param name="layername"></param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        public ObjectId Check(string layername);

        /// <summary>
        /// Ищет в таблице слоёв слой, связанный с объектом  с указанным именем и при его отсутствии добавляет его, 
        /// используя репозиторий с данными стандартных слоёв
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        public ObjectId Check(LayerWrapper layer) => Check(layer.LayerInfo);

        public ObjectId ForceCheck(string layerName);

        public bool TryFindLinetype(string? linetypename, out ObjectId lineTypeId);

    }
}
