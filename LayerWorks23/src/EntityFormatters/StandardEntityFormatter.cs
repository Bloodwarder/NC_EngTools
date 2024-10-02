using LayersIO.DataTransfer;
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
        private static IRepository<string, LayerProps> _repository;
        private static ILogger? _logger;

        public StandardEntityFormatter()
        {
            _repository = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
            _logger = LoaderCore.NcetCore.ServiceProvider.GetService<ILogger>();
        }
        public void FormatEntity(Entity entity)
        {
            string layerName = entity.Layer;
            var parser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
            string key = parser.GetLayerInfo(layerName).TrueName;
            FormatEntity(entity, key);
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


