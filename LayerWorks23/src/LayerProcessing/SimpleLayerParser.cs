using System;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, обрабатывающий только строки с именами без связанного объекта
    /// </summary>
    public class SimpleLayerParser : LayerParser
    {
        /// <inheritdoc/>
        public SimpleLayerParser(string layername) : base(layername) { }
        /// <summary>
        /// Не работает, так как парсер обрабатывает только строку без связанного объекта
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }
}


