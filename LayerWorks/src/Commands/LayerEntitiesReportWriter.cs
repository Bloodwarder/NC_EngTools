﻿//System
using System.IO;
//Microsoft
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
//nanoCAD
using Teigha.DatabaseServices;
using Teigha.Colors;
//Internal
using LoaderCore;
using LoaderCore.NanocadUtilities;
using LoaderCore.Interfaces;
using LayerWorks.LayerProcessing;
using NameClassifiers;

namespace LayerWorks.Commands
{
    public class LayerEntitiesReportWriter
    {
        private static readonly Color _byLayerColor = Color.FromColorIndex(ColorMethod.ByLayer, 256);
        private readonly IReportWriterFactory<LayerEntityReport>? _reportWriterFactory;
        public LayerEntitiesReportWriter(IReportWriterFactory<LayerEntityReport>? factory)
        {
            _reportWriterFactory = factory;
        }

        public void WriteReport()
        {
            // TODO: Пока сделано "в лоб", чтобы работало. Много вариантов для оптимизации. Вынести логику фильтрации в xml
            Workstation.Logger?.LogDebug("{ProcessingObject}: Начало подготовки отчёта", nameof(LayerEntitiesReportWriter));
            using (var transaction = Workstation.TransactionManager.StartTransaction())
            {
                BlockTable blocktable = (BlockTable)transaction.GetObject(Workstation.Database.BlockTableId, OpenMode.ForRead, false);
                BlockTableRecord modelspace = (BlockTableRecord)transaction.GetObject(blocktable![BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                var parser = NameParser.Current;
                var entities = modelspace.Cast<ObjectId>()
                                         .Select(id => (Entity)transaction.GetObject(id, OpenMode.ForRead))
                                         .Where(e => e.Layer.StartsWith(parser.Prefix + parser.Separator))
                                         .Where(e => e is Polyline && e.Color == _byLayerColor);
                HashSet<string?> filteredStatus = new() { "пр", "дем", "нреорг" };
                Dictionary<string, EntityLayerWrapper> wrappersDictionary = new();
                HashSet<string> wrongLayers = new();
                foreach (var entity in entities)
                {
                    string layerName = entity.Layer;
                    try
                    {
                        if (wrappersDictionary.ContainsKey(layerName))
                        {
                            wrappersDictionary[layerName].BoundEntities.Add(entity);
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Объект {Entity} слоя {Layer} добавлен к существующему LayerWrapper",
                                                         nameof(LayerEntitiesReportWriter),
                                                         entity.GetType().Name,
                                                         layerName);
                        }
                        else if (!wrongLayers.Contains(layerName))
                        {
                            var wrapper = new EntityLayerWrapper(entity);
                            if (wrapper.LayerInfo.IsValid
                                && filteredStatus.Contains(wrapper.LayerInfo.Status))
                                wrappersDictionary.Add(layerName, wrapper);
                            Workstation.Logger?.LogDebug("{ProcessingObject}: Объект {Entity} слоя {Layer} добавлен к новому LayerWrapper",
                                                         nameof(LayerEntitiesReportWriter),
                                                         entity.GetType().Name,
                                                         layerName);
                        }
                    }
                    catch (WrongLayerException ex)
                    {
                        Workstation.Logger?.LogDebug(ex,
                                                     "{ProcessingObject}: Слой {Layer} не обработан - {Message}",
                                                     nameof(LayerEntitiesReportWriter),
                                                     layerName,
                                                     ex.Message);
                        wrongLayers.Add(entity.Layer);
                        continue;
                    }
                }
                var wrappers = wrappersDictionary.Values;
                var reports = wrappers.GroupBy(w => new { w.LayerInfo.MainName, w.LayerInfo.Status, Reconstruction = w.LayerInfo.SuffixTagged["Reconstruction"] })
                                      .Select(group => new LayerEntityReport()
                                      {
                                          MainName = group.Key.MainName,
                                          Status = group.Key.Status,
                                          Reconstruction = (group.Key.Reconstruction
                                                            || group.Key.Status == "дем"
                                                            || group.Key.Status == "нреорг") ? "Переустройство" : "Новое строительство",
                                          CumulativeLength = group.SelectMany(w => w.BoundEntities)
                                                                      .Select(e => (Polyline)e)
                                                                      .Sum(pl => Math.Round(pl.Length / 1000, 3))
                                      })
                                      .OrderBy(r => r.MainName)
                                      .ThenBy(r => r.Reconstruction)
                                      .ThenBy(r => r.Status)
                                      .ToArray();
                string dwgPath = Workstation.Database.Filename;
                var dir = new FileInfo(dwgPath).Directory;
                var reportPath = Path.Combine(dir!.FullName, $"LayerReport_{DateTime.Now.ToLongTimeString().Replace(":", "-")}.xlsx");
                if (_reportWriterFactory == null)
                {
                    Workstation.Logger?.LogError("Ошибка. Не зарегистрирована служба подготовки отчётов");
                    return;
                }
                var writer = _reportWriterFactory.CreateWriter(reportPath, "Report");
                writer.PrepareReport(reports.ToArray());
                writer.ShowReport();
            }
        }


    }

    public class LayerEntityReport
    {
# nullable disable warnings
        public LayerEntityReport() { }
#nullable restore
        public string MainName { get; set; }
        public string Status { get; set; }
        public string Reconstruction { get; set; }
        public string Units { get; set; } = "км";
        public double CumulativeLength { get; set; }
    }
}
