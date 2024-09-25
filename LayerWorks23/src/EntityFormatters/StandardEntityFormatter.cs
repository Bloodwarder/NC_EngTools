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
        private static IRepository<string, LayerProps> _repository = LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IRepository<string, LayerProps>>();
        private static ILogger? _logger = LoaderCore.NcetCore.ServiceProvider.GetService<ILogger>();
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
                _logger?.LogWarning($"Не удалось форматировать объект чертежа {entity.GetType().Name}"); // UNDONE : Проверить что выводит. Создать корректное сообщение об ошибке
                return;
            }
            entity.LinetypeScale = props?.LTScale ?? entity.LinetypeScale;
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


