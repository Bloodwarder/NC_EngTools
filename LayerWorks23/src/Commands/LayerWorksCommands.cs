using LoaderCore.Utilities;
using Teigha.Runtime;

using LoaderCore;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.DataTransfer;
using LoaderCore.Interfaces;
using LoaderCore.SharedData;
using LayerWorks.DataRepositories;

namespace LayerWorks.Commands
{
    public static class LayerWorksCommands
    {
        static LayerWorksCommands()
        {
            LayerAlterer = NcetCore.ServiceProvider.GetRequiredService<LayerAlterer>();
            ChapterVisualizer = NcetCore.ServiceProvider.GetRequiredService<ChapterVisualizer>();
            LegendAssembler = NcetCore.ServiceProvider.GetRequiredService<LegendAssembler>();
            LayerEntitiesReportWriter = NcetCore.ServiceProvider.GetRequiredService<LayerEntitiesReportWriter>();
            AutoZoner = NcetCore.ServiceProvider.GetRequiredService<AutoZoner>();
            DrawOrderProcessor = NcetCore.ServiceProvider.GetRequiredService<DrawOrderProcessor>();
        }
        private static LayerAlterer LayerAlterer { get; }
        internal static ChapterVisualizer ChapterVisualizer { get; }
        private static LegendAssembler LegendAssembler { get; }
        private static LayerEntitiesReportWriter LayerEntitiesReportWriter { get; }
        private static AutoZoner AutoZoner { get; }
        private static DrawOrderProcessor DrawOrderProcessor { get; }


        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public static void LayerStatusChangeCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.LayerStatusChange);
        }

        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public static void LayerAlterCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.LayerAlter);
        }

        [CommandMethod("ДОПИНФО", CommandFlags.Redraw)]
        public static void AuxDataAssignCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.AuxDataAssign);
        }
        [CommandMethod("КАЛЬКА")]
        public static void TransparentOverlayToggleCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.TransparentOverlayToggle);
        }

        [CommandMethod("ПРЕФИКС")]
        public static void ChangePrefixCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.ChangePrefix);
        }

        [CommandMethod("ТЕГ", CommandFlags.Redraw)]
        public static void LayerTagCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.LayerTag);
        }

        [CommandMethod("СВС", CommandFlags.Redraw)]
        public static void StandartLayerValuesCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.StandartLayerValues);
        }

        [CommandMethod("СВСШТРИХ", CommandFlags.Redraw)]
        public static void StandartLayerHatchCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.StandartLayerHatch);
        }

        [CommandMethod("ВИЗРАЗДЕЛ")]
        public static void VisualizerComand()
        {
            NcetCommand.ExecuteCommand(ChapterVisualizer.Visualizer);
        }

        [CommandMethod("АВТОСБОРКА")]
        public static void AssembleCommand()
        {
            NcetCommand.ExecuteCommand(LegendAssembler.Assemble);
        }

        [CommandMethod("НОВСТАНДСЛОЙ")]
        public static void NewStandardLayerCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlterer.NewStandardLayer);
        }

        [CommandMethod("СЛОЙОТЧЕТ")]
        public static void WriteReportCommand()
        {
            NcetCommand.ExecuteCommand(LayerEntitiesReportWriter.WriteReport);
        }

        [CommandMethod("АВТОЗОНЫ", CommandFlags.UsePickSet)]
        public static void AutoZoneCommand()
        {
            NcetCommand.ExecuteCommand(AutoZoner.AutoZone);
        }

        [CommandMethod("ПОРЯДОКСЛОЁВ", CommandFlags.UsePickSet)]
        public static void ArrangeDrawOrderCommand()
        {
            NcetCommand.ExecuteCommand(DrawOrderProcessor.ArrangeEntities);
        }

        [CommandMethod("NCET_RELOAD")]
        public static void ReloadStandardData()
        {
            NcetCommand.ExecuteCommand(() =>
            {
                var prov1 = NcetCore.ServiceProvider.GetService<IRepository<string, LayerProps>>();
                var prov2 = NcetCore.ServiceProvider.GetService<IRepository<string, LegendData>>();
                var prov3 = NcetCore.ServiceProvider.GetService<IRepository<string, LegendDrawTemplate>>();
                var prov4 = NcetCore.ServiceProvider.GetService<IRepository<string, string>>();
                var prov5 = NcetCore.ServiceProvider.GetService<IRepository<string, ZoneInfo[]>>();
                if (prov1 is InMemoryRepository<string, LayerProps> imp1)
                    imp1.Reload();
                if (prov2 is InMemoryRepository<string, LegendData> imp2)
                    imp2.Reload();
                if (prov3 is InMemoryRepository<string, LegendDrawTemplate> imp3)
                    imp3.Reload();
                if (prov4 is InMemoryRepository<string, string> imp4)
                    imp4.Reload();
                if (prov5 is InMemoryRepository<string, ZoneInfo[]> imp5)
                    imp5.Reload();
            });
        }


    }
}
