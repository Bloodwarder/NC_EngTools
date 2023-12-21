using System;

namespace LayersIO.DataTransfer
{

    /// <summary>
    /// Словари к перезагрузке
    /// </summary>
    [Flags]
    public enum ToReload
    {
        /// <summary>
        /// Свойства
        /// </summary>
        Properties = 1,
        /// <summary>
        /// Альтернативные типы
        /// </summary>
        Alter = 2,
        /// <summary>
        /// Информация для компоновки условных обозначений
        /// </summary>
        Legend = 4,
        /// <summary>
        /// Информация для отрисовки условных обозначений
        /// </summary>
        LegendDraw = 8
    }
}