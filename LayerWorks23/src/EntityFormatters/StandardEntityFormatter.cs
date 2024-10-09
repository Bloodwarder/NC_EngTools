using LayersIO.DataTransfer;
using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public class StandardEntityFormatter : IEntityFormatter
    {
        private static readonly IRepository<string, LayerProps> _repository;
        private static readonly ILogger? _logger;

        static StandardEntityFormatter()
        {
            _repository = NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
            _logger = NcetCore.ServiceProvider.GetService<ILogger>();
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
                _logger?.LogDebug("Форматирование объекта {EntityType} слоя {LayerName} не выполнено. Не подходящий слой", entity.GetType().Name, layerName);
                foreach (Exception ex in layerInfoResult.GetExceptions())
                    _logger?.LogTrace(ex, "Ошибка: {exceptionMessage}", ex.Message);
                return;
            }
        }

        public void FormatEntity(Entity entity, string key)
        {
            bool success = _repository.TryGet(key, out LayerProps? props);
            if (!success)
            {
                _logger?.LogWarning($"Не удалось форматировать {entity.GetType().Name}");
                return;
            }
            entity.LinetypeScale = props?.LinetypeScale ?? entity.LinetypeScale;
            switch (entity)
            {
                case Polyline polyline:
                    polyline.ConstantWidth = props?.ConstantWidth ?? polyline.ConstantWidth;
                    break;
                case Hatch hatch:
                    _logger?.LogWarning($"Форматирование штриховок не реализовано"); // UNDONE : Реализовать форматирование штриховок
                    break;
            }
        }
    }

}


