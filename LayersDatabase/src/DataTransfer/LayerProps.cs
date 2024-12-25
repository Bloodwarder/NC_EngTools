namespace LayersIO.DataTransfer
{

    /// <summary>
    /// Свойства слоя
    /// </summary>
    public class LayerProps
    {
        /// <summary>
        /// Глобальная ширина
        /// </summary>
        public double? ConstantWidth;
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        public double? LinetypeScale;
        /// <summary>
        /// Красный
        /// </summary>
        public byte? Red;
        /// <summary>
        /// Зелёный
        /// </summary>
        public byte? Green;
        /// <summary>
        /// Синий
        /// </summary>
        public byte? Blue;
        /// <summary>
        /// Тип линий
        /// </summary>
        public string? LinetypeName;
        /// <summary>
        /// Вес линий
        /// </summary>
        public int? LineWeight;
        /// <summary>
        /// Индекс для сортировки в DrawOrderTable чертежа
        /// </summary>
        public int DrawOrderIndex;

    }
}