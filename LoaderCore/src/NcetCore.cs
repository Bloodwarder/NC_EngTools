using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using HostMgd.ApplicationServices;
using Teigha.Runtime;

using LoaderCore.Integrity;
using LoaderCore.UI;
using LoaderCore.Utilities;
using LoaderCore.Interfaces;

namespace LoaderCore
{
    /// <summary>
    /// Загрузчик сборок с окном конфигурации
    /// </summary>
    public static class NcetCore
    {
        private const string ConfigurationXmlFileName = "Configuration.xml";
        private static readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private static bool _serviceProviderBuilt = false;
        private static IServiceProvider _serviceProvider = null!;

        // Поиск обновлений и настройка загрузки и обновления сборок
        static NcetCore()
        {
        }
        public static IServiceProvider ServiceProvider
        {
            get => _serviceProviderBuilt ? _serviceProvider : throw new InvalidOperationException("Провайдер сервисов ещё не построен");
            private set
            {
                _serviceProvider = value;
                _serviceProviderBuilt = true;
            }
        }
        public static IServiceCollection Services => !_serviceProviderBuilt ?
                                                        _serviceCollection :
                                                        throw new InvalidOperationException("Провайдер сервисов уже построен");
        internal static ILogger? Logger { get; private set; }
        internal static List<ModuleHandler> Modules { get; } = new();
        internal static string RootLocalDirectory { get; set; } = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.FullName;
        internal static string? RootUpdateDirectory { get; set; }
        private static Dictionary<string, string> LibraryFiles { get; } = new();

        public static void Initialize()
        {
            Logger = new NcetEditorConsoleLogger();

            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            UpdateInfoFiles();
            UpdateExternalUpdater();

            DisplayAutorunConfigurationWindow(configurationXml);
            // Заново читаем конфиг (мог измениться)
            configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            InitializeModules(configurationXml);
            UpdateDataFolder();
            UpdateIntermediateFolder();
            IndexAssemblies();
            RegisterNcDependencies();
            RegisterCommonDependencies();
            PostInitializeModules();
        }

        /// <summary>
        /// Инициализация сборок NCEngTools приложением, не зависящем от nanoCAD
        /// </summary>
        /// <param name="registerCallersServices">Делегат регистрации сервисов запускающего приложения</param>
        public static void InitializeAsLibrary(Action<IServiceCollection>? registerCallersServices = null)
        {
            registerCallersServices?.Invoke(Services);

            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            UpdateInfoFiles();
            InitializeModules(configurationXml);
            UpdateDataFolder();
            UpdateIntermediateFolder();
            IndexAssemblies();
            RegisterCommonDependencies();
            PostInitializeModules();
        }

        private static void CheckConfigurationXml(XDocument document)
        {
            // проверить по xsd
            bool isValid = true;
            XmlSchemaSet xmlSchemaSet = new();
            xmlSchemaSet.Add(null, Path.Combine(RootLocalDirectory, "ExtensionLibraries", "LoaderCore", "ConfigurationSchema.xsd"));
            document.Validate(xmlSchemaSet, (sender, e) =>
            {
                Logger?.LogWarning("{ProcessingObject}: {Message}", nameof(NcetCore), e.Message);
                isValid = false;
            });

            DirectoryInfo defaultLocalDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!;

            // ищем в файле конфигурации путь к источнику обновлений
            XElement updateDirectoryElement = document.Root!.Element("Directories")!.Element("UpdateDirectory")!;
            DirectoryInfo updateDir = new(updateDirectoryElement.Value);
            if (!updateDir.Exists)
                throw new IOException("Невозможно найти папку с обновлениями");
            RootUpdateDirectory = updateDir.FullName;

            // если файл конфигурации не соответствует схеме - перезаписать файл
            if (!isValid)
            {
                var sourceFile = updateDir.GetFiles(ConfigurationXmlFileName, SearchOption.AllDirectories).Single();
                var targetFile = defaultLocalDir.GetFiles(ConfigurationXmlFileName, SearchOption.AllDirectories).Single();
                sourceFile.CopyTo(targetFile.FullName, true);
            }

            // проверить и прописать локальную директорию
            // везде восклицательные знаки, так как уже проверили по xsd
            XElement localDirectoryElement = document.Root!.Element("Directories")!.Element("LocalDirectory")!;

            DirectoryInfo? localDir = !string.IsNullOrEmpty(localDirectoryElement.Value) ? new(localDirectoryElement.Value) : null;

            bool isUpdated = false;

            if (localDir == null || !localDir.Exists)
            {
                localDir = defaultLocalDir;
                localDirectoryElement.Value = localDir.FullName;
                RootLocalDirectory = localDir.FullName;
                isUpdated = true;
            }
            // проверить и прописать директории с пользовательскими данными
            var defaultUserDataPath = localDir.GetDirectories("UserData").Single();
            var parserPath = document.Root.Element("LayerWorksConfiguration")!
                                          .Element("NameParserPaths")!
                                          .Elements()
                                          .Single(e => e.Element("Type")!.Value == "Local");
            var standardPath = document.Root.Element("LayerWorksConfiguration")!
                                          .Element("LayerStandardPaths")!
                                          .Elements()
                                          .Single(e => e.Element("Type")!.Value == "Local");
            if (parserPath.Value == null || !Directory.Exists(parserPath.Value))
            {
                parserPath.Element("Path")!.Value = defaultUserDataPath.FullName;
                isUpdated = true;
            }
            if (standardPath.Value == null || !Directory.Exists(standardPath.Value))
            {
                standardPath.Element("Path")!.Value = defaultUserDataPath.FullName;
                isUpdated = true;
            }
            // сохранить файл, если был обновлён
            if (isUpdated)
                document.Save(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));
        }

        /// <summary>
        /// Обновление файлов со списком команд, списком обновлений и известными проблемами
        /// </summary>
        private static void UpdateInfoFiles() => UpdateDirectory(RootLocalDirectory, RootUpdateDirectory, SearchOption.AllDirectories, "*.txt", "*.md");
        private static void UpdateExternalUpdater() => 
            UpdateDirectory(RootLocalDirectory,
                            RootUpdateDirectory,
                            SearchOption.AllDirectories,
                            "NcetExternalUpdater.deps.json",
                            "NcetExternalUpdater.dll",
                            "NcetExternalUpdater.runtimeconfig.json",
                            "NcetExternalUpdater.exe");

        private static void UpdateDataFolder()
        {
            if (RootUpdateDirectory == null)
            {
                return;
            }
            var updateDir = Path.Combine(RootUpdateDirectory, "UserData");
            var localDir = Path.Combine(RootLocalDirectory, "UserData");
            UpdateDirectory(localDir, updateDir, SearchOption.AllDirectories);
        }

        private static void UpdateIntermediateFolder()
        {
            if (RootUpdateDirectory == null)
            {
                return;
            }
            var updateDir = Path.Combine(RootUpdateDirectory, "ExtensionLibraries");
            var localDir = Path.Combine(RootLocalDirectory, "ExtensionLibraries");
            UpdateDirectory(localDir, updateDir, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Отобразить стартовое окно и дать пользователю отредактировать конфигурацию
        /// </summary>
        /// <param name="configurationXml"></param>
        private static void DisplayAutorunConfigurationWindow(XDocument configurationXml)
        {
            // Читаем конфигурацию на предмет необходимости отображения стартового окна и отображаем его
            bool showStartUp = XmlConvert.ToBoolean(configurationXml.Root!.Element("ShowStartup")!.Value);
            if (showStartUp)
            {
                string configXmlPath = Path.Combine(RootLocalDirectory, ConfigurationXmlFileName);
                StartUpWindow window = new(configXmlPath);
                Application.ShowModalWindow(window);
            }
        }

        /// <summary>
        /// Загрузить сборки и их зависимости, при необходимости обновить
        /// </summary>
        /// <param name="configurationXml"></param>
        private static void InitializeModules(XDocument configurationXml)
        {
            XElement[] moduleElements = configurationXml.Root!.Elements().Where(e => e.Attribute("Module") != null).ToArray();
            foreach (XElement element in moduleElements)
            {
                ModuleHandler module = new(element.Attribute("Module")!.Value);
                Modules.Add(module);
                bool update = XmlConvert.ToBoolean(element.Element("UpdateEnabled")!.Value);
                if (update)
                {
                    module.Update();
                }
                bool enable = XmlConvert.ToBoolean(element.Element("Enabled")!.Value);
                if (enable)
                {
                    module.Load();
                    Logger?.LogInformation("Модуль {ModuleName} загружен", module.Name);
                }
            }
        }

        /// <summary>
        /// Инициализировать словарь сборок dll
        /// </summary>
        private static void IndexAssemblies()
        {
            var dllFiles = new DirectoryInfo(RootLocalDirectory).GetFiles("*.dll", SearchOption.AllDirectories);
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    LibraryFiles.TryAdd(AssemblyName.GetAssemblyName(dllFile.FullName).FullName, dllFile.FullName);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
            }
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        private static void RegisterNcDependencies()
        {
            Services.AddSingleton<ILogger, NcetEditorConsoleLogger>()
                    .AddTransient<ILogger<NcetCommand>, NcetFileCommandLogger>();
        }

        private static void RegisterCommonDependencies()
        {
            var configPath = Path.Combine(RootLocalDirectory, ConfigurationXmlFileName);
            IConfiguration config = new ConfigurationBuilder().AddXmlFile(configPath, optional: false, reloadOnChange: true)
#if DEBUG
                                                              .AddUserSecrets(Assembly.GetExecutingAssembly())
#endif
                                                              .Build();

            Services.AddSingleton(config)
                    .AddSingleton<IFilePathProvider, PathProvider>();

            ServiceProvider = Services.BuildServiceProvider();
            _serviceProviderBuilt = true;

            Logger = ServiceProvider.GetRequiredService<ILogger>();
        }

        private static void PostInitializeModules()
        {
            foreach (ModuleHandler module in Modules)
            {
                module.PostInitialize();
            }
        }

        private static Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                string filename = args.Name;
                bool getAssemblySuccess = LibraryFiles!.TryGetValue(filename, out string? assemblyPath);
                if (getAssemblySuccess)
                    return Assembly.LoadFrom(assemblyPath!);
            }
            catch
            {
                return null;
            }
            return null;
        }

        private static void UpdateDirectory(string localPath, string? sourcePath, SearchOption searchOption, params string[] filters)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                Logger?.LogWarning("Ошибка обновления - не указана директория источник обновлений");
                return;
            }

            var updateDir = new DirectoryInfo(sourcePath);
            var localDir = new DirectoryInfo(localPath);

            static Dictionary<string, FileInfo> GetDirectoryTextFiles(DirectoryInfo dir, SearchOption searchOption, string[] filters)
            {
                if (filters == null || !filters.Any())
                {
                    return dir.GetFiles("*", searchOption).ToDictionary(fi => fi.Name);
                }
                else
                {
                    List<FileInfo> list = new();
                    foreach (string filter in filters)
                        list.AddRange(dir.GetFiles(filter, searchOption));
                    return list.ToDictionary(fi => fi.Name);
                }
            }

            var updateDict = GetDirectoryTextFiles(updateDir, searchOption, filters);
            var localDict = GetDirectoryTextFiles(localDir, searchOption, filters);

            foreach (string file in updateDict.Keys)
            {
                try
                {
                    if (!localDict.ContainsKey(file) || localDict[file].LastWriteTime < updateDict[file].LastWriteTime)
#if !DEBUG
                        updateDict[file].CopyTo(updateDict[file].FullName.Replace(sourcePath, localPath), true);
#endif
                        Logger?.LogWarning("Обновление файла \"{File}\"", file);

                }
                catch (System.Exception ex)
                {
                    Logger?.LogWarning(ex, "Ошибка обновления файла \"{File}\":\n{Exception}", file, ex.Message);
                    continue;
                }
            }

            foreach (string file in localDict.Keys)
            {
                if (!updateDict.ContainsKey(file))
                    try
                    {
#if !DEBUG
                        localDict[file].Delete();
#endif
                        Logger?.LogWarning("Удаление файла \"{File}\"", file);

                    }
                    catch (System.Exception ex)
                    {
                        Logger?.LogWarning(ex, "Ошибка удаления файла \"{File}\":\n{Exception}", file, ex.Message);
                        continue;
                    }
            }
        }

        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        public static void ConfigureAutorun()
        {
            var pathProvider = ServiceProvider.GetRequiredService<IFilePathProvider>();
            StartUpWindow window = new(pathProvider.GetPath(ConfigurationXmlFileName));
            window.bUpdateLayerWorks.IsEnabled = false;
            window.bUpdateUtilities.IsEnabled = false;
            window.bUpdateGeoMod.IsEnabled = false;
            Application.ShowModalWindow(window);
        }
    }
}
