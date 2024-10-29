using LayersIO.DataTransfer;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public class StandardEntityFormatter : IEntityFormatter
    {
        private readonly IRepository<string, LayerProps> _repository;

        public StandardEntityFormatter(IRepository<string, LayerProps> repository)
        {
            _repository = repository;
        }
        public void FormatEntity(Entity entity)
        {
            string layerName = entity.Layer;
            var parser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
            var layerInfoResult = parser.GetLayerInfo(layerName);
            if (layerInfoResult.Status == LayerInfoParseStatus.Success)
            {
                string key = layerInfoResult.Value.TrueName;
                FormatEntity(entity, key);
            }
            else
            {
                Workstation.Logger?.LogDebug("Форматирование объекта {EntityType} слоя {LayerName} не выполнено. Не подходящий слой", entity.GetType().Name, layerName);
                foreach (Exception ex in layerInfoResult.GetExceptions())
                    Workstation.Logger?.LogTrace(ex, "Ошибка: {exceptionMessage}", ex.Message);
                return;
            }
        }

        public void FormatEntity(Entity entity, string key)
        {
            bool success = _repository.TryGet(key, out LayerProps? props);
            if (!success)
            {
                Workstation.Logger?.LogDebug("Не удалось форматировать {EntityName}", entity.GetType().Name);
                return;
            }
            entity.LinetypeScale = props?.LinetypeScale ?? entity.LinetypeScale;
            switch (entity)
            {
                case Polyline polyline:
                    polyline.ConstantWidth = props?.ConstantWidth ?? polyline.ConstantWidth;
                    break;
                case Hatch hatch:
                    Workstation.Logger?.LogWarning($"Форматирование штриховок не реализовано"); // UNDONE : Реализовать форматирование штриховок
                    break;
            }
        }
    }

}


