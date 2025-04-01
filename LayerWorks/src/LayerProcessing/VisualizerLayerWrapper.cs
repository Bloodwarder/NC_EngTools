using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord, хранящий данные об исходном цвете и видимости слоя
    /// </summary>
    public class VisualizerLayerWrapper : RecordLayerWrapper
    {
        const byte redproj = 0; const byte greenproj = 255; const byte blueproj = 255;
        const byte redns = 0; const byte greenns = 153; const byte bluens = 153;

        /// <inheritdoc/>
        public VisualizerLayerWrapper(LayerTableRecord ltr) : base(ltr)
        {
            StoredLayerProps = ReadLayerProps();
            VisualizerLayerWrappers.Add(this);
            LayerStandartizedEvent += (o, e) =>
            {
                if ((LayerTableRecord)o! == BoundLayer)
                    StoredLayerProps = ReadLayerProps();
            };
            BoundLayer.Erased += (sender, e) => VisualizerLayerWrappers.StoredLayerStates[Workstation.Document].Remove(this); // TODO: Протестировать удаление из очереди обработки при удалении слоя
        }

        public StatedLayerProps StoredLayerProps { get; set; }

        public static void Create(LayerTableRecord record)
        {
            _ = new VisualizerLayerWrapper(record);
        }

        /// <summary>
        /// Возврат исходного цвета и видимости слоя
        /// </summary>
        public void Reset()
        {
            WriteLayerProps(StoredLayerProps);
        }
        /// <summary>
        /// Возврат исходного цвета и видимости слоя
        /// </summary>
        public override void Push()
        {
            Reset();
        }

        /// <summary>
        /// Принимает тип объектов. Если объект не относится к заданному типу - выключает его. Если относится к переустройству - задаёт яркий цвет
        /// </summary>
        /// <param name="primaryClassifier">Тип объекта по основному классификатору</param>
        public void Push(string? primaryClassifier, List<string> highlitedStatusList)
        {
            if (primaryClassifier == null)
            {
                Reset();
                return;
            }

            if (LayerInfo.PrimaryClassifier == primaryClassifier)
            {
                BoundLayer.IsOff = false;
                if (LayerInfo.SuffixTagged["Reconstruction"])
                {
                    if (LayerInfo.Status == highlitedStatusList[0])
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redproj, greenproj, blueproj);
                    }
                    else if (LayerInfo.Status == highlitedStatusList[1])
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redns, greenns, bluens);
                    }
                }
            }
            else
            {
                BoundLayer.IsOff = true;
            }
        }

        public void Push(StatedLayerProps statedLayerProps)
        {
            WriteLayerProps(statedLayerProps);
        }
    }
}


