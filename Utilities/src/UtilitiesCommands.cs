using LoaderCore.Utilities;
using Teigha.Runtime;

using static Utilities.CoordinateParser;
using static Utilities.EntityPointPolylineTracer;
using static Utilities.Labeler;
using static Utilities.TextProcessor;
using static Utilities.MultilineConverter;
using static Utilities.VerticalCalc;
using static Utilities.PolylineUnderlayer;
using static Utilities.BrightnessShifter;

namespace Utilities
{
    public static class UtilitiesCommands
    {
        [CommandMethod("ПОДПИСЬ")]
        public static void LabelDrawCommand()
        {
            NcetCommand.ExecuteCommand(LabelDraw);
        }

        [CommandMethod("ТОРИЕНТ", CommandFlags.UsePickSet)]
        public static void TextOrientBy2PointsCommand()
        {
            NcetCommand.ExecuteCommand(TextOrientBy2Points);
        }

        [CommandMethod("ПЛТОРИЕНТ", CommandFlags.UsePickSet)]
        public static void TextOrientByPolilineSegmentCommand()
        {
            NcetCommand.ExecuteCommand(TextOrientByPolylineSegment);
        }

        [CommandMethod("СМТ", CommandFlags.Redraw)]
        public static void StripMTextCommand()
        {
            NcetCommand.ExecuteCommand(StripMText);
        }

        [CommandMethod("ОБЪЕКТСОЕД")]
        public static void TracePolylineCommand()
        {
            NcetCommand.ExecuteCommand(TracePolyline);
        }

        [CommandMethod("МЛИНВПЛИН", CommandFlags.UsePickSet)]
        public static void ConvertMultilineToPolylineCommand()
        {
            NcetCommand.ExecuteCommand(ConvertMultilineToPolyline);
        }

        [CommandMethod("НУЛЕВАЯШИРИНАТЕКСТА", CommandFlags.Redraw)]
        public static void AssignZeroWidthCommand()
        {
            NcetCommand.ExecuteCommand(AssignZeroWidth);
        }

        [CommandMethod("ПЛЭКСЕЛЬ", CommandFlags.UsePickSet)]
        public static void ExcelCoordinatesToPolylineCommand()
        {
            NcetCommand.ExecuteCommand(ExcelCoordinatesToPolyline);
        }

        [CommandMethod("УКЛОН")]
        public static void SlopeCalcCommand()
        {
            NcetCommand.ExecuteCommand(SlopeCalc);
        }

        [CommandMethod("СЛ_ОТМ")]
        public static void NextMarkCommand()
        {
            NcetCommand.ExecuteCommand(NextMark);
        }

        [CommandMethod("СР_ОТМ")]
        public static void AverageLevelCommand()
        {
            NcetCommand.ExecuteCommand(AverageLevel);
        }

        [CommandMethod("ГОРИЗ_РАСЧ")]
        public static void HorizontalCalcCommand()
        {
            NcetCommand.ExecuteCommand(IsolinesCalc);
        }

        [CommandMethod("КРАСН_ЧЕРН_УРАВН", CommandFlags.Redraw)]
        public static void RedBlackEqualCommand()
        {
            NcetCommand.ExecuteCommand(RedBlackEqual);
        }

        [CommandMethod("ПЛПОДЛОЖКА", CommandFlags.Redraw)]
        public static void CreateUnderlayingPolylineCommand()
        {
            NcetCommand.ExecuteCommand(CreateUnderlayingPolyline);
        }

        [CommandMethod("СДВЯРКОСТЬ", CommandFlags.Redraw)]
        public static void BrightnessShiftCommand()
        {
            NcetCommand.ExecuteCommand(BrightnessShift);
        }
    }

}
