using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.DependencyInjection;

using HostMgd.ApplicationServices;
using Teigha.Runtime;

using LoaderCore.Integrity;
using LoaderCore.UI;
using LoaderCore.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml.Schema;

namespace LoaderCore
{
    public static class LoaderExtension
    {
        public static void Initialize()
        {
            var coreHandler = new ModuleHandler("LoaderCore");

            // TODO : обновить txt

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.Initialize();
        }

        public static void InitializeAsLibrary()
        {
            var coreHandler = new ModuleHandler("LoaderCore");

            // TODO : обновить txt

            coreHandler.Update();
            coreHandler.Load();
            NcetCore.InitializeAsLibrary();
        }
    }


    /// <summary>
    /// Загрузчик сборок с окном конфигурации
    /// </summary>
    public static class NcetCore
    {
        private const string ConfigurationXmlFileName = "Configuration.xml";
        private static readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private static bool _serviceProviderBuilt = false;
        private static IServiceProvider serviceProvider = null!;

        // Поиск обновлений и настройка загрузки и обновления сборок
        static NcetCore()
        {
        }
        public static IServiceProvider ServiceProvider
        {
            get => _serviceProviderBuilt ? serviceProvider : throw new InvalidOperationException("Провайдер сервисов ещё не построен");
            private set
            {
                serviceProvider = value;
                _serviceProviderBuilt = true;
            }
        }
        public static IServiceCollection Services => !_serviceProviderBuilt ?
                                                        _serviceCollection :
                                                        throw new InvalidOperationException("Провайдер сервисов уже построен");
        internal static List<ModuleHandler> Modules { get; } = new();
        internal static string RootLocalDirectory { get; set; } = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.Parent!.Parent!.FullName;
        internal static string? RootUpdateDirectory { get; set; }
        private static Dictionary<string, string> LibraryFiles { get; } = new();

        public static void Initialize()
        {
            LoggingRouter.WriteLog += Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage;

            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            DisplayAutorunConfigurationWindow(configurationXml);

            // Заново читаем конфиг (мог измениться)
            configurationXml = XDocument.Load(ConfigurationXmlFileName);

            InitializeModules(configurationXml);
            IndexFiles();
            RegisterDependencies();
            LoggingRouter.WriteLog = null;
        }

        private static void CheckConfigurationXml(XDocument document)
        {
            // проверить по xsd
            bool isValid = true;
            XmlSchemaSet xmlSchemaSet = new();
            xmlSchemaSet.Add(null, Path.Combine(RootLocalDirectory, "ExtensionLibraries", "LoaderCore", "ConfigurationSchema.xsd"));
            document.Validate(xmlSchemaSet, (sender, e) =>
            {
                LoggingRouter.WriteLog?.Invoke(e.Message);
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
                    LoggingRouter.WriteLog?.Invoke($"Модуль {module.Name} загружен");
                }
            }
        }

        private static void IndexFiles()
        {
            PathProvider.InitializeStructure(RootLocalDirectory);
            var dllFiles = new DirectoryInfo(RootLocalDirectory).GetFiles("*.dll", SearchOption.AllDirectories);
            foreach (var dllFile in dllFiles)
            {
                LibraryFiles.TryAdd(AssemblyName.GetAssemblyName(dllFile.FullName).FullName, dllFile.FullName);
            }
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        private static void RegisterDependencies()
        {

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddXmlFile(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName), optional: false, reloadOnChange: true);
            IConfiguration config = configurationBuilder.Build();

            Services.AddSingleton(config)
                    .AddSingleton<ILogger, NcetSimpleLogger>();

            ServiceProvider = Services.BuildServiceProvider();
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

        public static void InitializeAsLibrary()
        {
            XDocument configurationXml = XDocument.Load(Path.Combine(RootLocalDirectory, ConfigurationXmlFileName));

            CheckConfigurationXml(configurationXml);
            InitializeModules(configurationXml);
            IndexFiles();
            RegisterDependencies();
        }

        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        [CommandMethod("NCET_CONFIG")]
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
