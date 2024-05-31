using LoaderCore.Configuration;
using NameClassifiers;
using NanocadUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LoaderCore.Configuration.Configuration;

namespace LayerWorks
{
    internal static class FallbackPath
    {
        internal static string? DocumentOverride
        {
            get
            {
                FileInfo document = new(Workstation.Document.Name); // UNDONE: проверить какое выдаёт имя.
                FileInfo? target = document.Directory?.GetFiles($"{document.Name}*{LayerWrapper.StandartPrefix}*.db").FirstOrDefault();
                return target?.FullName;
            }
        }

        internal static string? UserOverride => LayerWorksConfiguration.NameParserPaths?.FirstOrDefault(lwp => lwp.Route == PathRoute.Overrides)?.Path;
        internal static string LocalBackUp => LayerWorksConfiguration.NameParserPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Local).Path;
        internal static string CommonShared => LayerWorksConfiguration.NameParserPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Shared).Path;
    }
}
