using HostMgd.ApplicationServices;
using LoaderCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private const string RelatedConfigSection = "LayerWorksConfiguration";
        private static LayerWorksConfiguration _configuration = 
            LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<LayerWorksConfiguration>(RelatedConfigSection);
        internal static string? DocumentOverride
        {
            get
            {
                FileInfo document = new(Workstation.Document.Name); // UNDONE: проверить какое выдаёт имя.
                FileInfo? target = document.Directory?.GetFiles($"{document.Name}*{LayerWrapper.StandartPrefix}*.db").FirstOrDefault();
                return target?.FullName;
            }
        }

        internal static string? UserOverride => _configuration.LayerStandardPaths?.FirstOrDefault(lwp => lwp.Route == PathRoute.Overrides)?.Path;
        internal static string LocalBackUp => _configuration.LayerStandardPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Local).Path;
        internal static string CommonShared => _configuration.LayerStandardPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Shared).Path;
    }
}
