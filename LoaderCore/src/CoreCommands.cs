using LoaderCore.NanocadUtilities;
using LoaderCore.UI;
using LoaderCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teigha.Runtime;

namespace LoaderCore
{
    public static class CoreCommands
    {
        [CommandMethod("ЛОГКОМАНДЫ")]
        public static void LogCommand()
        {
            Workstation.IsCommandLoggingEnabled = true;
        }

        [CommandMethod("NCET_CONFIG")]
        public static void ConfigureAutorunCommand()
        {
            NcetCommand.ExecuteCommand(NcetCore.ConfigureAutorun);
        }

    }
}
