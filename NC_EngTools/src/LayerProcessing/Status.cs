namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Статус объекта
    /// </summary>
    public enum Status : int
    {
        /// <summary>
        /// Существующий
        /// </summary>
        Existing = 0,
        /// <summary>
        /// Демонтируемый
        /// </summary>
        Deconstructing = 1,
        /// <summary>
        /// Планируемый
        /// </summary>
        Planned = 2,
        /// <summary>
        /// Демонтируемый-неутверждаемый
        /// </summary>
        NSDeconstructing = 3,
        /// <summary>
        /// Неутверждаемый-планируемый реорганизуемый
        /// </summary>
        NSReorg = 4,
        /// <summary>
        /// Неутверждаемый-планируемый
        /// </summary>
        NSPlanned = 5
    }
}


