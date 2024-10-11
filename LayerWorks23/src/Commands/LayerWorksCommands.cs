using LoaderCore.Utilities;
using Teigha.Runtime;

using static LayerWorks.Commands.LayerAlterer;
using static LayerWorks.Commands.ChapterVisualizer;
using static LayerWorks.Commands.LegendAssembler;
using static LayerWorks.Commands.LayerEntitiesReportWriter;

namespace LayerWorks.Commands
{
    public static class LayerWorksCommands
    {
        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public static void LayerStatusChangeCommand()
        {
            NcetCommand.ExecuteCommand(LayerStatusChange);
        }

        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public static void LayerAlterCommand()
        {
            NcetCommand.ExecuteCommand(LayerAlter);
        }
        [CommandMethod("ДОПИНФО", CommandFlags.Redraw)]
        public static void AuxDataAssignCommand()
        {
            NcetCommand.ExecuteCommand(AuxDataAssign);
        }
        [CommandMethod("КАЛЬКА")]
        public static void TransparentOverlayToggleCommand()
        {
            NcetCommand.ExecuteCommand(TransparentOverlayToggle);
        }
        [CommandMethod("ПРЕФИКС")]
        public static void ChangePrefixCommand()
        {
            NcetCommand.ExecuteCommand(ChangePrefix);
        }
        [CommandMethod("ТЕГ", CommandFlags.Redraw)]
        public static void LayerTagCommand()
        {
            NcetCommand.ExecuteCommand(LayerTag);
        }
        [CommandMethod("СВС", CommandFlags.Redraw)]
        public static void StandartLayerValuesCommand()
        {
            NcetCommand.ExecuteCommand(StandartLayerValues);
        }
        [CommandMethod("ВИЗРАЗДЕЛ")]
        public static void VisualizerComand()
        {
            NcetCommand.ExecuteCommand(Visualizer);
        }
        [CommandMethod("АВТОСБОРКА")]
        public static void AssembleCommand()
        {
            NcetCommand.ExecuteCommand(Assemble);
        }

        [CommandMethod("НОВСТАНДСЛОЙ")]
        public static void NewStandardLayerCommand()
        {
            NcetCommand.ExecuteCommand(NewStandardLayer);
        }

        [CommandMethod("СЛОЙОТЧЕТ")]
        public static void WriteReportCommand()
        {
            NcetCommand.ExecuteCommand(WriteReport);
        }

    }
}
