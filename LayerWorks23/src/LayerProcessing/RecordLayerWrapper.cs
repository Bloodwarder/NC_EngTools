
using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord
    /// </summary>
    public class RecordLayerWrapper : LayerWrapper
    {
        private static IRepository<string, LayerProps> _repository;
        private DBObjectWrapper<LayerTableRecord> _boundLayer = null!;

        protected static EventHandler? LayerStandartizedEvent;
        
        static RecordLayerWrapper()
        {
            _repository = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
        }
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
            bool success = _repository.TryGet(LayerInfo.TrueName, out var props);
            if (!success)
                throw new NoPropertiesException($"Отсутствует стандарт для слоя {BoundLayer.Name}");
            WriteLayerProps(props!);
            LayerStandartizedEvent?.Invoke(BoundLayer, new());
        }

        internal StatedLayerProps ReadLayerProps()
        {
            var transaction = Workstation.TransactionManager.TopTransaction;
            _ = _repository.TryGet(LayerInfo.TrueName, out var standard);
            StatedLayerProps layerProps = standard?.ToStatedLayerProps() ?? new();

            layerProps.LinetypeName = ((LinetypeTableRecord)transaction.GetObject(BoundLayer.LinetypeObjectId, OpenMode.ForRead)).Name;
            layerProps.Red = BoundLayer.Color.Red;
            layerProps.Green = BoundLayer.Color.Green;
            layerProps.Blue = BoundLayer.Color.Blue;
            layerProps.LineWeight = (int)BoundLayer.LineWeight;
            layerProps.IsOff = BoundLayer.IsOff;
            layerProps.IsFrozen = BoundLayer.IsFrozen;
            layerProps.IsLocked = BoundLayer.IsLocked;
            return layerProps;
        }

        internal void WriteLayerProps(LayerProps layerProps)
        {
            _ = LayerChecker.TryFindLinetype(layerProps.LinetypeName, out ObjectId lttrId);
            try
            {
                BoundLayer.Color = layerProps.GetColor() ?? BoundLayer.Color;
                BoundLayer.LinetypeObjectId = lttrId;
                BoundLayer.LineWeight = (LineWeight)(layerProps.LineWeight ?? -3);
                if (layerProps is StatedLayerProps statedLayerProps)
                {
                    BoundLayer.IsOff = statedLayerProps.IsOff ?? BoundLayer.IsOff;
                    BoundLayer.IsFrozen = statedLayerProps.IsFrozen ?? BoundLayer.IsFrozen;
                    BoundLayer.IsLocked = statedLayerProps.IsLocked ?? BoundLayer.IsLocked;
                }
            }
            catch
            {
                Workstation.Editor.WriteMessage($"Не удалось придать свойства слою {BoundLayer.Name}");
            }
        }


    }
}


