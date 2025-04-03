using HostMgd.ApplicationServices;
using LoaderCore.NanocadUtilities;
using LoaderCore.UI;
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
            string mdPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Команды.md", SearchOption.AllDirectories).Single();
            string stylesPath = Directory.GetFiles(NcetCore.RootLocalDirectory, "Styles.css", SearchOption.AllDirectories).Single();
            var html = MdToHtmlConverter.Convert(mdPath, stylesPath);
            InfoDisplayWindow window = new(html, "Список команд");
            Application.ShowModalWindow(window);
        }

    }
}
