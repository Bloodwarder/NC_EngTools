namespace LayerWorks.Legend
{
    /// <summary>
    /// Фильтр для компоновщика сетки легенды
    /// </summary>
    public enum TableFilter
    {
        /// <summary>
        /// Только существующие
        /// </summary>
        ExistingOnly,
        /// <summary>
        /// Полная таблица
        /// </summary>
        Full,
        /// <summary>
        /// Только существующие и утверждаемые проектные / демонтируемые сети
        /// </summary>
        InternalOnly,
        /// <summary>
        /// Две отдельных таблицы с утверждаемыми и не утверждаемыми сетями
        /// </summary>
        InternalAndExternal,
        /// <summary>
        /// Таблица для утверждаемых объектов и отдельные таблицы для каждого имени внешнего проекта
        /// </summary>
        InternalAndSeparatedExternal
    }
}
