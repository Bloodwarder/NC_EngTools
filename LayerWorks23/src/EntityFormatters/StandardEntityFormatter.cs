using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NameClassifiers;
using Teigha.DatabaseServices;

namespace LayerWorks.EntityFormatters
{
    public class StandardEntityFormatter : IEntityFormatter
    {
        private static IStandardReader<LayerProps> _standardReader = LoaderCore.LoaderExtension.ServiceProvider.GetRequiredService<IStandardReader<LayerProps>>();
        public void FormatEntity(Entity entity)
        {
            string layerName = entity.Layer;
            var parser = NameParser.LoadedParsers[LayerWrapper.StandartPrefix!];
            string key = parser.GetLayerInfo(layerName).TrueName;
            FormatEntity(entity, key);
        }

        public void FormatEntity(Entity entity, string key)
        {
            bool success = _standardReader.TryGetStandard(key, out LayerProps? props);
            if (!success)
            {
                Logger.WriteLog?.Invoke($"Не удалось форматировать объект чертежа {entity}"); // UNDONE : Проверить что выводит. Создать корректное сообщение об ошибке
                return;
            }
            entity.LinetypeScale = props?.LTScale ?? entity.LinetypeScale;
            switch (entity)
            {
                case Polyline polyline:
                    polyline.ConstantWidth = props?.ConstantWidth ?? polyline.ConstantWidth;
                    break;
                case Hatch hatch:
                    Logger.WriteLog?.Invoke($"Форматирование штриховок не реализовано"); // UNDONE : Реализовать форматирование штриховок
                    break;
            }
        }
    }

}


