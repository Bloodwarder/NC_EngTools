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

        [CommandMethod("ВКТИМПОРТ")]
        public static void GeometryFromClipboardWktCommand()
        {
            NcetCommand.ExecuteCommand(_wktCommands.GeometryFromClipboardWkt);
        }

        [CommandMethod("ЗОНА", CommandFlags.UsePickSet)]
        public static void SimpleZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.SimpleZone);
        }

        [CommandMethod("ЗОНАДИФФ", CommandFlags.UsePickSet)]
        public static void DiverseZoneCommand()
        {
            NcetCommand.ExecuteCommand(_bufferizationCommands.DiverseZone);
        }

        //[CommandMethod("ЗОНААВТО", CommandFlags.UsePickSet)]
        //public static void AutoZoneCommand()
        //{
        //    NcetCommand.ExecuteCommand(_bufferizationCommands.AutoZone);
        //}

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
