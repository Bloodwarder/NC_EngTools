using LoaderCore.Integrity;

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
            var coreHandler = new ModuleHandler("LoaderCore");

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.Initialize();
        }

        public static void InitializeAsLibrary()
        {
            // НЕ ВСТАВЛЯТЬ СЮДА КОД С ЗАВИСИМОСТЯМИ
            var coreHandler = new ModuleHandler("LoaderCore");

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.InitializeAsLibrary();
        }
    }
}
