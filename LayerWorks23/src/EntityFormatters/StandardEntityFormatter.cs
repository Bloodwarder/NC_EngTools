using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;
using NameClassifiers;
using System.Text.RegularExpressions;
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
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            bool parserGetSuccess = NameParser.LoadedParsers.TryGetValue(prefix, out var parser);
            if (!parserGetSuccess)
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Форматирование объекта {EntityType} слоя {LayerName} не выполнено. Не загружен парсер",
                                             nameof(StandardEntityFormatter),
                                             entity.GetType().Name,
                                             layerName);
                return;
            }
            var layerInfoResult = parser?.GetLayerInfo(layerName);
            if (layerInfoResult!.Status == LayerInfoParseStatus.Success)
            {
                string key = layerInfoResult.Value.TrueName;
                FormatEntity(entity, key);
            }
            else
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Форматирование объекта {EntityType} слоя {LayerName} не выполнено. Не подходящий слой",
                                             nameof(StandardEntityFormatter),
                                             entity.GetType().Name,
                                             layerName);
                foreach (Exception ex in layerInfoResult!.GetExceptions())
                    Workstation.Logger?.LogTrace(ex, "{ProcessingObject}: Ошибка: {exceptionMessage}", nameof(StandardEntityFormatter), ex.Message);
                return;
            }
        }

        public void FormatEntity(Entity entity, string key)
        {
            bool success = _repository.TryGet(key, out LayerProps? props);
            if (!success)
            {
                Workstation.Logger?.LogDebug("{ProcessingObject}: Не удалось форматировать {EntityName}", nameof(StandardEntityFormatter), entity.GetType().Name);
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


