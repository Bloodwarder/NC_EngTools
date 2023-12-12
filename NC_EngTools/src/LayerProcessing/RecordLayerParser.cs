﻿using Loader.CoreUtilities;
using System;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord
    /// </summary>
    public class RecordLayerParser : LayerParser
    {
        private DBObjectWrapper<LayerTableRecord> _boundLayer;


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
        public RecordLayerParser(LayerTableRecord ltr) : base(ltr.Name)
        {
            BoundLayer = ltr;
        }
        /// <summary>
        /// Изменяет имя и свойства связанного слоя слоя
        /// </summary>
        /// <exception cref="NotImplementedException">Метод не реализован (пока не понадобился)</exception>
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }
}

