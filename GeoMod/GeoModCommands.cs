using LoaderCore.Utilities;
using Teigha.Runtime;

using static GeoMod.GeoProcessing;

namespace GeoMod
{
    public static class GeoModCommands
    {
        [CommandMethod("ВКТЭКСПОРТ", CommandFlags.UsePickSet)]
        public static void WktToClipboardCommand()
        {
            NcetCommand.ExecuteCommand(WktToClipboard);
        }

        [CommandMethod("ВКТИМПОРТ")]
        public static void GeometryFromClipboardWktCommand()
        {
            NcetCommand.ExecuteCommand(GeometryFromClipboardWkt);
        }

        [CommandMethod("ЗОНА", CommandFlags.UsePickSet)]
        public static void SimpleZoneCommand()
        {
            NcetCommand.ExecuteCommand(SimpleZone);
        }

        [CommandMethod("ЗОНАДИФФ", CommandFlags.UsePickSet)]
        public static void DiverseZoneCommand()
        {
            NcetCommand.ExecuteCommand(DiverseZone);
        }

        [CommandMethod("ЗОНОБЪЕД", CommandFlags.UsePickSet)]
        public static void ZoneJoinCommand()
        {
            NcetCommand.ExecuteCommand(ZoneJoin);
        }

        [CommandMethod("ОКРУГЛКООРД", CommandFlags.UsePickSet)]
        public static void ReduceCoordinatePrecisionCommand()
        {
            NcetCommand.ExecuteCommand(ReduceCoordinatePrecision);
        }



    }
}
