using LoaderCore;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Teigha.Runtime;

namespace GeoMod.Commands
{
    public static class GeoModCommands
    {
        private static readonly GeomodBufferizationCommands _bufferizationCommands;
        private static readonly GeomodPrecisionCommands _precisionCommands;
        private static readonly GeomodWktCommands _wktCommands;
        static GeoModCommands()
        {
            _bufferizationCommands = NcetCore.ServiceProvider.GetRequiredService<GeomodBufferizationCommands>();
            _precisionCommands = NcetCore.ServiceProvider.GetRequiredService<GeomodPrecisionCommands>();
            _wktCommands = NcetCore.ServiceProvider.GetRequiredService<GeomodWktCommands>();
        }


        [CommandMethod("ВКТЭКСПОРТ", CommandFlags.UsePickSet)]
        public static void WktToClipboardCommand()
        {
            NcetCommand.ExecuteCommand(_wktCommands.WktToClipboard);
        }

        [CommandMethod("ВКТМУЛЬТИЭКСПОРТ", CommandFlags.UsePickSet)]
        public static void WktMultigeomToClipboardCommand()
        {
            NcetCommand.ExecuteCommand(_wktCommands.WktMultiGeometryToClipboard);
        }

        [CommandMethod("ВКТИМПОРТ")]
        public static void GeometryFromClipboardWktCommand()
        {
            NcetCommand.ExecuteCommand(_wktCommands.GeometryFromClipboardWkt);
        }

        [CommandMethod("ФИЧИМПОРТ")]
        public static void FeatureFromClipboardCommand()
        {
            NcetCommand.ExecuteCommand(_wktCommands.FeatureFromClipboard);
        }

        [CommandMethod("ЗОНА", CommandFlags.UsePickSet)]
        public static void SimpleZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.SimpleZone);
        }

        [CommandMethod("ЗОНАТОЧ", CommandFlags.UsePickSet)]
        public static void PointZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.PointZone);
        }
        [CommandMethod("ЗОНАСОКР", CommandFlags.UsePickSet)]
        public static void ReducedLinearZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.ReducedLinearZone);
        }

        [CommandMethod("ЗОНАДИФФ", CommandFlags.UsePickSet)]
        public static void DiverseZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.DiverseZone);
        }


        [CommandMethod("ЗОНОБЪЕД", CommandFlags.UsePickSet)]
        public static void ZoneJoinCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.ZoneJoin);
        }

        [CommandMethod("ОКРУГЛКООРД", CommandFlags.UsePickSet)]
        public static void ReduceCoordinatePrecisionCommand()
        {
            NcetCommand.ExecuteCommand(_precisionCommands.ReduceCoordinatePrecision);
        }
    }
}
