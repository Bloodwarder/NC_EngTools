using LoaderCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Windows;

namespace WindowTestLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
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
            base.OnStartup(e);
        }

        private static void InitializeLoaderCore()
        {
            LibraryLoaderExtension.InitializeAsLibrary();
        }

    }
}
