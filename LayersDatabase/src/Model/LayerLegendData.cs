namespace LayersIO.Model
{
    /// <summary>
    /// Собственнный тип LayerGroupData, содержащий информацию для сборки легенды
    /// </summary>
    public class LayerLegendData
    {
        /// <summary>
        /// Ранг. Меньше - отображается выше
        /// </summary>
        public int Rank { get; set; }
        /// <summary>
        /// Текст в легенде
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// Текст в легенде для подраздела
        /// </summary>
        public string? SubLabel { get; set; }
        /// <summary>
        /// Показывает, нужно ли компоновщику игнорировать указанный слой
        /// </summary>
        public bool IgnoreLayer { get; set; } = false;
    }
}
