using LoaderCore.NanocadUtilities;
using LoaderCore.Utilities;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        [CommandMethod("КОМАНДЫ_NCET")]
        public static void ShowCommandsListCommand()
        {
            string path = new DirectoryInfo(NcetCore.RootLocalDirectory).GetFiles("Команды.txt").Single().FullName;
            Process.Start("notepad.exe", path);
        }

    }
}
