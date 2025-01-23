using LayersDatabaseEditor.Utilities;
using LayersDatabaseEditor.ViewModel;
using LoaderCore;

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
            try
            {
                Thread.Sleep(10000);
                DirectoryInfo? dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
                DirectoryInfo mainDirectory = dir.Parent!.Parent!;
                string path = Path.Combine(dir!.Parent!.FullName, "LoaderCore", "LoaderCore.dll");
                string diLibPath = Path.Combine(dir!.Parent!.FullName, "LoaderCore", "Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                Assembly.LoadFrom(path);
                Assembly.LoadFrom(diLibPath);
                InitializeLoaderCore();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }       
        }
        private static void InitializeLoaderCore()
        {
            LibraryLoaderExtension.InitializeAsLibrary(RegisterServices);
        }

        
        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, EditorWindowLogger>()
                    .AddTransient<EditorViewModel>();
        }
    }
}
