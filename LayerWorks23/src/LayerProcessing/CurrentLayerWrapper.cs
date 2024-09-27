using LayersIO.DataTransfer;
using LayersIO.ExternalData;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using NanocadUtilities;
using System.Security.Cryptography.X509Certificates;
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
            var layerInfo = GetInfoFromString(layerName, out string? ex);
            if (layerInfo != null)
            {
                StandartizeCurrentValues(layerInfo);
            }
            else
            {
                ILogger? logger = NcetCore.ServiceProvider.GetService<ILogger>();
                logger?.LogWarning(ex);
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


