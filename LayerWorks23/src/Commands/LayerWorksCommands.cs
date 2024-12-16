using LoaderCore.Utilities;
using Teigha.Runtime;

using LoaderCore;
using Microsoft.Extensions.DependencyInjection;

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
        }
        private static LayerAlterer LayerAlterer { get; }
        internal static ChapterVisualizer ChapterVisualizer { get; }
        private static LegendAssembler LegendAssembler { get; }
        private static LayerEntitiesReportWriter LayerEntitiesReportWriter { get; }
        private static AutoZoner AutoZoner { get; }


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

        [CommandMethod("ЗОНААВТО", CommandFlags.UsePickSet)]
        public static void AutoZoneCommand()
        {
            NcetCommand.ExecuteCommand(AutoZoner.AutoZone);
        }


    }
}
