using NameClassifiers;
using LoaderCore.NanocadUtilities;
using System.IO;

namespace LayerWorks
{
    internal static class FallbackPath
    {
        private const string RelatedConfigSection = "LayerWorksConfiguration";
        //private static LayerWorksConfiguration _configuration = 
        //    LoaderCore.NcetCore.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<LayerWorksConfiguration>(RelatedConfigSection);
        internal static string? DocumentOverride
        {
            get
            {
                FileInfo document = new(Workstation.Database.Filename);
                FileInfo? target = document.Directory?.GetFiles($"{document.Name}*{NameParser.Current.Prefix}*.db").FirstOrDefault();
                return target?.FullName;
            }
        }

        //internal static string? UserOverride => _configuration.LayerStandardPaths?.FirstOrDefault(lwp => lwp.Route == PathRoute.Overrides)?.Path;
        //internal static string LocalBackUp => _configuration.LayerStandardPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Local).Path;
        //internal static string CommonShared => _configuration.LayerStandardPaths.FirstOrDefault(lwp => lwp.Route == PathRoute.Shared).Path;
    }
}
