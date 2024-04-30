
using Teigha.DatabaseServices;
using NameClassifiers;
using NanocadUtilities;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord
    /// </summary>
    public class RecordLayerWrapper : LayerWrapper
    {
        private DBObjectWrapper<LayerTableRecord> _boundLayer = null!;


        /// <summary>
        /// Связанный слой (объект LayerTableRecord)
        /// </summary>
        public LayerTableRecord BoundLayer
        {
            get => _boundLayer.Get();
            set => _boundLayer = new DBObjectWrapper<LayerTableRecord>(value, OpenMode.ForWrite);
        }

        /// <summary>
        /// Конструктор, принимающий объект LayerTableRecord
        /// </summary>
        /// <param name="ltr">Запись таблицы слоёв</param>
        public RecordLayerWrapper(LayerTableRecord ltr) : base(ltr.Name)
        {
            BoundLayer = ltr;
        }
        /// <summary>
        /// Изменяет имя и свойства связанного слоя слоя
        /// </summary>
        /// <exception cref="NotImplementedException">Метод не реализован (пока не понадобился)</exception>
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }
}


