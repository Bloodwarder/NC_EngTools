using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord, хранящий данные об исходном цвете и видимости слоя
    /// </summary>
    public class ChapterStoreLayerWrapper : RecordLayerParser
    {
        const byte redproj = 0; const byte greenproj = 255; const byte blueproj = 255;
        const byte redns = 0; const byte greenns = 153; const byte bluens = 153;
        /// <summary>
        /// Исходное состояние видимости слоя
        /// </summary>
        public bool StoredEnabledState;
        /// <summary>
        /// Исходный цвет слоя
        /// </summary>
        public Teigha.Colors.Color StoredColor;
        /// <inheritdoc/>
        public ChapterStoreLayerWrapper(LayerTableRecord ltr) : base(ltr)
        {
            StoredEnabledState = ltr.IsOff;
            StoredColor = ltr.Color;
            ChapterStoredLayerWrappers.Add(this);
        }
        /// <summary>
        /// Возврат исходного цвета и видимости слоя
        /// </summary>
        public void Reset()
        {
            //BoundLayer = Workstation.TransactionManager.TopTransaction.GetObject(BoundLayer.Id, OpenMode.ForWrite) as LayerTableRecord;
            BoundLayer.IsOff = StoredEnabledState;
            BoundLayer.Color = StoredColor;
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
        /// <param name="primaryClassifier">Тип объекта</param>
        public void Push(string primaryClassifier)
        {
            if (primaryClassifier == null)
            {
                Reset();
                return;
            }

            if (LayerInfo.PrimaryClassifier == primaryClassifier)
            {
                BoundLayer.IsOff = false;
                if (recstatus)
                {
                    if (BuildStatus == Status.Planned)
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redproj, greenproj, blueproj);
                    }
                    else if (BuildStatus == Status.NSPlanned)
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
    }
}


