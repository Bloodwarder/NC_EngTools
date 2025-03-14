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
        private const string NoneHatchPatternString = "None";
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
                string key = layerInfoResult.Value!.TrueName;
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
                    FormatHatch(hatch, key);
                    break;
            }
        }

        private void FormatHatch(Hatch hatch, string key)
        {
            bool success = _drawRepository.TryGet(key, out LegendDrawTemplate? drawTemplate);
            if (!success)
                return;
            if (string.IsNullOrEmpty(drawTemplate!.InnerHatchPattern) || drawTemplate!.InnerHatchPattern == NoneHatchPatternString)
                return;
            Action<Hatch>? delayedBackgroundSetAction = null;
            if (drawTemplate.InnerHatchPattern != null && drawTemplate.InnerHatchPattern != "SOLID")
            {
                hatch.PatternAngle = drawTemplate.InnerHatchAngle * Math.PI / 180;
                hatch.PatternScale = drawTemplate.InnerHatchScale;
                if (drawTemplate.InnerHatchBrightness != 0)
                {
                    var color = hatch.LayerId.GetObject<LayerTableRecord>(OpenMode.ForRead).Color;

                    if (hatch.PatternName != "SOLID")
                        hatch.BackgroundColor = color.BrightnessShift(drawTemplate.InnerHatchBrightness);
                    else
                        // если исходный образец штриховки - SOLID - отложить назначение фонового цвета до изменения образца
                        delayedBackgroundSetAction = h => h.BackgroundColor = color.BrightnessShift(drawTemplate.InnerHatchBrightness);
                }
            }
            else
            {
                var color = hatch.LayerId.GetObject<LayerTableRecord>(OpenMode.ForRead).Color;
                hatch.Color = color.BrightnessShift(drawTemplate.InnerHatchBrightness);
            }

            //ДИКИЙ БЛОК, ПЫТАЮЩИЙСЯ ОБРАБОТАТЬ ОШИБКИ ДЛЯ НЕПОНЯТНЫХ ШТРИХОВОК
            // BUG: Не назначает штриховку SOLID при более чем одном объекте в Loop
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
            delayedBackgroundSetAction?.Invoke(hatch);
        }
    }

}


