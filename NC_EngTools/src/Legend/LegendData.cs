namespace LayerWorks.Legend
{
    /// <summary>
    /// Данные слоя для компоновки легенды
    /// </summary>
    public struct LegendData
    {
        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank;
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label;
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string SubLabel;
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer;
    }
}
