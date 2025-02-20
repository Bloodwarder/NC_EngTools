using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics;
using Teigha.Runtime;

namespace LoaderCore
{
    public class NcExtensionHandler : IExtensionApplication
    {
        internal static HashSet<string> FailedFileUpdates = new();
        public void Initialize()
        {
            // Здесь ничего не надо - всё и так инициализируется независимо от нанокада
        }

        public void Terminate()
        {
            var provider = NcetCore.ServiceProvider.GetService<IFilePathProvider>();
            var updater = provider?.GetPath("NcetExternalUpdater.exe");
            
            if (updater != null)
                Process.Start(updater, "NC_EngTools_StartUp.dll");
        }
    }
}
