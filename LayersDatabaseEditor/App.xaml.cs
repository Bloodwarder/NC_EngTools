using LayersDatabaseEditor.Utilities;
using LayersDatabaseEditor.ViewModel;
using LoaderCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
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
                DirectoryInfo? dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location)!;
                DirectoryInfo mainDirectory = dir.Parent!.Parent!;
                string path = Path.Combine(dir!.Parent!.FullName, "LoaderCore", "LoaderCore.dll");
                string diLibPath = Path.Combine(dir!.Parent!.FullName, "LayerWorks", "Microsoft.Extensions.DependencyInjection.Abstractions.dll");
                var context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
                Assembly.LoadFrom(path);
                context.LoadFromAssemblyPath(diLibPath);
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
