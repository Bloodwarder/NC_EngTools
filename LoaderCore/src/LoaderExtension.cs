using HostMgd.ApplicationServices;
using LoaderCore.Integrity;
using LoaderCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LoaderCore
{
    public static class LoaderExtension
    {
        /// <summary>
        /// Инициализатор, обновляющий и загружающий зависимости сборки этого модуля
        /// </summary>
        public static void Initialize()
        {
            // НЕ ВСТАВЛЯТЬ СЮДА КОД С ЗАВИСИМОСТЯМИ
            Action<string> logMethod = Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage;
            PreInitializeSimpleLogger.RegisterLogMethod(logMethod);

            var coreHandler = new ModuleHandler("LoaderCore");

            coreHandler.Update();
            coreHandler.Load();

            PreInitializeSimpleLogger.ClearLoggers();

            NcetCore.Initialize();
        }
    }

    public static class LibraryLoaderExtension
    { 
        public static void InitializeAsLibrary()
        {
            // НЕ ВСТАВЛЯТЬ СЮДА КОД С ЗАВИСИМОСТЯМИ
            var coreHandler = new ModuleHandler("LoaderCore");

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.InitializeAsLibrary();
        }

        public static void InitializeAsLibrary(Action<IServiceCollection>? registerCallersServices)
        {
            // НЕ ВСТАВЛЯТЬ СЮДА КОД С ЗАВИСИМОСТЯМИ
            var coreHandler = new ModuleHandler("LoaderCore");

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.InitializeAsLibrary(registerCallersServices);
        }
    }
}
