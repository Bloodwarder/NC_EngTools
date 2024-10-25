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
        public static ILogger? Logger { get; private set; }
        internal static List<ModuleHandler> Modules { get; } = new();
        internal static string RootLocalDirectory { get; set; } = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.FullName;
        internal static string? RootUpdateDirectory { get; set; }
        private static Dictionary<string, string> LibraryFiles { get; } = new();

        public static void Initialize()
        {
            ILogger logger = new NcetEditorConsoleLogger();
            Services.AddSingleton(logger);
            Logger = logger;
            LoggingRouter.RegisterLogMethod(LogLevel.Information, Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage);

            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            UpdateInfoFiles();

            DisplayAutorunConfigurationWindow(configurationXml);
            // Заново читаем конфиг (мог измениться)
            configurationXml = XDocument.Load(ConfigurationXmlFileName);

            InitializeModules(configurationXml);
            IndexFiles();
            RegisterDependencies();
            PostInitializeModules();

            //ReloadLoggerNc();

        }

        public static void InitializeAsLibrary()
        {
            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            UpdateInfoFiles();
            InitializeModules(configurationXml);
            IndexFiles();
            RegisterDependencies();
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
                Logger?.LogWarning(e.Message);
                isValid = false;
            });

            DirectoryInfo defaultLocalDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!;

            // если ошибочный - перезаписать файл
            if (!isValid)
            {
                XElement updateDirectoryElement = document.Root!.Element("Directories")!.Element("UpdateDirectory")!;
                DirectoryInfo updateDir = new(updateDirectoryElement.Value);
                if (!updateDir.Exists)
                    throw new IOException("Невозможно найти папку с обновлениями");
                var sourceFile = updateDir.GetFiles(ConfigurationXmlFileName, SearchOption.AllDirectories).Single();
                var targetFile = defaultLocalDir.GetFiles(ConfigurationXmlFileName, SearchOption.AllDirectories).Single();
                RootUpdateDirectory = updateDir.FullName;
                sourceFile.CopyTo(targetFile.FullName, true);
            }

            // проверить и прописать локальную директорию
            // везде восклицательные знаки, так как уже проверили по xsd
            XElement localDirectoryElement = document.Root!.Element("Directories")!.Element("LocalDirectory")!;

            DirectoryInfo? localDir =  !string.IsNullOrEmpty(localDirectoryElement.Value) ? new(localDirectoryElement.Value) : null;

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
        private static void UpdateInfoFiles()
        {
            if (RootUpdateDirectory == null)
            {
                return;
            }
            var updateDir = new DirectoryInfo(RootUpdateDirectory);
            var localDir = new DirectoryInfo(RootLocalDirectory);
            var updateTxt = updateDir.GetFiles(".txt", SearchOption.TopDirectoryOnly).ToDictionary(fi => fi.Name);
            var localTxt = localDir.GetFiles(".txt", SearchOption.TopDirectoryOnly).ToDictionary(fi => fi.Name);
            foreach(var file in updateTxt.Keys)
            {
                try 
                {
                    if (!localTxt.ContainsKey(file) || localTxt[file].LastWriteTime < updateTxt[file].LastWriteTime)
                        updateTxt[file].CopyTo(localTxt[file].FullName, true);
                }
                catch (System.Exception ex)
                {
                    Logger?.LogWarning($"Ошибка обновления файла \"{file}\":\n{ex.Message}");
                    continue;
                }
                if (!localTxt.ContainsKey(file) || localTxt[file].LastWriteTime < updateTxt[file].LastWriteTime)
                    updateTxt[file].CopyTo(localTxt[file].FullName, true);
            }
        }

        private static void DisplayAutorunConfigurationWindow(XDocument configurationXml)
        {
            // Читаем конфигурацию на предмет необходимости отображения стартового окна и отображаем его
            bool showStartUp = XmlConvert.ToBoolean(configurationXml.Root!.Element("ShowStartup")!.Value);
            if (showStartUp)
            {
                StartUpWindow window = new(ConfigurationXmlFileName);
                Application.ShowModalWindow(window);
            }
        }

        private static void InitializeModules(XDocument configurationXml)
        {
            XElement[] moduleElements = configurationXml.Root!.Elements().Where(e => e.Attribute("Module") != null).ToArray();
            foreach (XElement element in moduleElements)
            {
                ModuleHandler module = new ModuleHandler(element.Attribute("Module")!.Value);
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
                    Logger?.LogInformation($"Модуль {module.Name} загружен");
                }
            }
        }

        private static void IndexFiles()
        {
            PathProvider.InitializeStructure(RootLocalDirectory);
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

        private static void RegisterDependencies()
        {

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddXmlFile(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName), optional: false, reloadOnChange: true);
            IConfiguration config = configurationBuilder.Build();

            Services.AddSingleton(config)
                    .AddTransient<ILogger<NcetCommand>>(provider => new NcetFileCommandLogger());

            //.AddSingleton<ILogger, NcetSimpleLogger>();

            ServiceProvider = Services.BuildServiceProvider();
        }


        /// <summary>
        /// Перезагрузка логгера с командной строкой nanoCAD
        /// </summary>
        private static void ReloadLoggerNc()
        {
            IConfiguration config = ServiceProvider.GetRequiredService<IConfiguration>();
            LoggingRouter.ClearLoggers();
            LogLevel editorLevel = config.GetSection("Logging").GetValue<LogLevel>("EditorLogLevel");
            LogLevel commandLevel = config.GetSection("Logging").GetValue<LogLevel>("CommandLogLevel");
            LoggingRouter.RegisterLogMethod(editorLevel, Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage);
            // TODO: Реализовать логгирование команд в файл по необходимости
            //LoggingRouter.RegisterLogMethod(commandLevel, IMPLEMENT SINGLE COMMAND FILE LOGGING);
        }

        private static void ReloadLoggerLib()
        {
            LoggingRouter.ClearLoggers();
        }

        private static void PostInitializeModules()
        {
            foreach (var module in Modules)
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



        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        public static void ConfigureAutorun()
        {
            StartUpWindow window = new(PathProvider.GetPath(ConfigurationXmlFileName));
            window.bUpdateLayerWorks.IsEnabled = false;
            window.bUpdateUtilities.IsEnabled = false;
            window.bUpdateGeoMod.IsEnabled = false;
            Application.ShowModalWindow(window);
        }
    }
}
