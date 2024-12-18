using LayersDatabaseEditor.Utilities;
using LayersDatabaseEditor.ViewModel;
using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Windows;

namespace LayersDatabaseEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DirectoryInfo? dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
            string path = Path.Combine(dir!.Parent!.FullName,"LoaderCore","LoaderCore.dll");
            string diLibPath = Path.Combine(dir!.Parent!.FullName, "LoaderCore", "Microsoft.Extensions.DependencyInjection.Abstractions.dll");
            Assembly.LoadFrom(path);
            Assembly.LoadFrom(diLibPath);
            InitializeLoaderCore();
        }
        private static void InitializeLoaderCore()
        {
            LoaderExtension.InitializeAsLibrary(RegisterServices);
        }

        
        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, EditorWindowLogger>()
                    .AddTransient<EditorViewModel>();
        }
    }
}
