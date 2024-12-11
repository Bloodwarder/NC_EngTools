using Teigha.DatabaseServices;

namespace LoaderCore.Interfaces
{
    public interface ILayerChecker
    {
        /// <summary>
        /// Ищет в таблице слоёв слой с указанным именем и при его отсутствии добавляет его, 
        /// </summary>
        /// <param name="layername">Имя слоя</param>
        /// <returns>ObjectId найденного или добавленного слоя (объекта LayerTableRecord)</returns>
        public ObjectId Check(string layername);
    }
}
