﻿using LayersIO.DataTransfer;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;
using LayerWorks.EntityFormatters;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с текущим слоем
    /// </summary>
    public class CurrentLayerWrapper : LayerWrapper
    {
        private static readonly LayerChecker _checker;
        private static readonly IRepository<string, LayerProps> _repository;

        static CurrentLayerWrapper()
        {
            _checker = NcetCore.ServiceProvider.GetRequiredService<LayerChecker>();
            _repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
        }

        /// <summary>
        /// Конструктор без параметров, автоматически передающий в базовый конструктор имя текущего слоя
        /// </summary>
        public CurrentLayerWrapper() : base(Clayername())
        {
            ActiveWrappers.Add(this);
        }

        private static string Clayername()
        {
            Database db = Workstation.Database;
            LayerTableRecord? ltr = db.TransactionManager.TopTransaction.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
            return ltr!.Name;
        }

        /// <summary>
        /// Задаёт стандартные свойства для черчения новых объектов чертежа
        /// </summary>
        public override void Push()
        {
            Database db = Workstation.Database;

            db.Clayer = _checker.Check(this);

            StandartizeCurrentValues(LayerInfo);
        }

        public static void DirectPush()
        {
            string layerName = Clayername();
            var layerInfoResult = GetInfoFromString(layerName);
            if (layerInfoResult.Status == LayerInfoParseStatus.Success)
            {
                StandartizeCurrentValues(layerInfoResult.Value);
            }
            else
            {
                var exception = layerInfoResult.GetExceptions().First();
                Workstation.Logger?.LogWarning(exception, "{Message}", exception.Message);
            }
        }

        private static void StandartizeCurrentValues(LayerInfo layerInfo)
        {
            bool success = _repository.TryGet(layerInfo.TrueName, out LayerProps? lp);
            if (success)
            {
                Workstation.Database.Celtscale = lp?.LinetypeScale ?? default;
                Workstation.Database.Plinewid = lp?.ConstantWidth ?? default;
            }
        }
    }
}


