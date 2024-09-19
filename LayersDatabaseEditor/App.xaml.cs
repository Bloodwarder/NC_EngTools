﻿using LoaderCore;
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
            string path = Path.Combine(dir!.Parent!.FullName, "LoaderCore.dll");
            Assembly.LoadFrom(path);
            InitializeLoaderCore();
        }

        private static void InitializeLoaderCore()
        {
            NcetCore.InitializeAsLibrary();
        }
    }


}
