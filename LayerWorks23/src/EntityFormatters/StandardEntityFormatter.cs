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
        private readonly IRepository<string, LayerProps> _propsRepository;
        private readonly IRepository<string, LegendDrawTemplate> _drawRepository;

        public StandardEntityFormatter(IRepository<string, LayerProps> repository, IRepository<string, LegendDrawTemplate> drawRepository)
        {
            _propsRepository = repository;
            _drawRepository = drawRepository;
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
                    Workstation.Logger?.LogDebug(ex, "{ProcessingObject}: Ошибка: {exceptionMessage}", nameof(StandardEntityFormatter), ex.Message);
                return;
            }
        }

        public void FormatEntity(Entity entity, string key)
        {
            bool success = _propsRepository.TryGet(key, out LayerProps? props);
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
                    FormatHatch(hatch);
                    break;
            }
        }

        private void FormatHatch(Hatch hatch)
        {
            bool success = _drawRepository.TryGet(hatch.Layer, out var drawTemplate);
            if (!success)
                return;
            if (drawTemplate!.InnerHatchPattern != null && drawTemplate.InnerHatchPattern != "SOLID")
            {
                hatch.PatternAngle = drawTemplate.InnerHatchAngle * Math.PI / 180;
                hatch.PatternScale = drawTemplate.InnerHatchScale;
                if (drawTemplate.InnerHatchBrightness != 0)
                    hatch.BackgroundColor = hatch.Color.BrightnessShift(drawTemplate.InnerHatchBrightness);
            }
            else
            {
                hatch.Color = hatch.Color.BrightnessShift(drawTemplate.InnerHatchBrightness);
            }
            //ДИКИЙ БЛОК, ПЫТАЮЩИЙСЯ ОБРАБОТАТЬ ОШИБКИ ДЛЯ НЕПОНЯТНЫХ ШТРИХОВОК
            try
            {
                hatch.SetHatchPattern(HatchPatternType.PreDefined, drawTemplate.InnerHatchPattern);
            }
            catch
            {

                for (int i = 2; i > -1; i--)
                {
                    try
                    {
                        hatch.SetHatchPattern((HatchPatternType)i, drawTemplate.InnerHatchPattern);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }
    }

}


