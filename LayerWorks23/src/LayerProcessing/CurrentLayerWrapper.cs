﻿using LayersIO.DataTransfer;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using LoaderCore.NanocadUtilities;
using Teigha.DatabaseServices;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер, связанный с текущим слоем
    /// </summary>
    public class CurrentLayerWrapper : LayerWrapper
    {
        /// <summary>
        /// Конструктор без параметров, автоматически передающий в базовый конструктор имя текущего слоя
        /// </summary>
        public CurrentLayerWrapper() : base(Clayername())
        {
            ActiveLayerWrappers.Add(this);
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

            db.Clayer = LayerChecker.Check(this);

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
                ILogger? logger = NcetCore.ServiceProvider.GetService<ILogger>();
                var exception = layerInfoResult.GetExceptions().First();
                logger?.LogWarning(exception, "{Message}", exception.Message);
            }
        }

        private static void StandartizeCurrentValues(LayerInfo layerInfo)
        {
            var service = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
            bool success = service.TryGet(layerInfo.TrueName, out LayerProps? lp);
            if (success)
            {
                Workstation.Database.Celtscale = lp?.LinetypeScale ?? default;
                Workstation.Database.Plinewid = lp?.ConstantWidth ?? default;
            }
        }
    }
}


