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

namespace LoaderCore
{
    /// <summary>
    /// Загрузчик сборок с окном конфигурации
    /// </summary>
    public static class LoaderExtension
    {
        // TODO: Переработать окно автозапуска и конфигурации 
        const string StructureXmlName = "Structure.xml";
        const string StartUpConfigName = "StartUpConfig.xml";
        private const string ConfigurationXmlFileName = "Configuration.xml";
        private static readonly IServiceCollection _serviceCollection = new ServiceCollection();
        private static bool _serviceProviderBuilt = false;
        private static IServiceProvider serviceProvider = null!;

        // Поиск обновлений и настройка загрузки и обновления сборок
        static LoaderExtension()
        {
            DirectoryInfo? dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.Parent;
            IEnumerable<FileInfo>? files = SearchDirectoryForDlls(dir!);
            LibraryFiles = SearchDirectoryForDlls(dir!)?.ToDictionary(f => f.Name, f => f.FullName);
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
        private static Dictionary<string, string>? LibraryFiles { get; }

        public static void Initialize()
        {
            LoggingRouter.WriteLog += Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage;


            List<ComparedFiles> files = InitializeFileStructure();

            // Читаем конфигурацию на предмет необходимости отображения стартового окна и отображаем его
            XDocument StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));
            bool showStartUp = XmlConvert.ToBoolean(StartUpConfig.Root.Element("StartUpShow").Attribute("Enabled")?.Value);
            if (showStartUp)
            {
                StartUpWindow window = new(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
                Application.ShowModalWindow(window);
            }

            // Заново читаем конфиг (мог измениться)
            StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));

            // Получаем из конфига теги обновляемых модулей и обновляем все необходимые файлы, помеченные указанными тегами
            List<string>? updatedModules = StartUpConfig
                .Root?
                .Element("Modules")?
                .Elements()
                .Where(e => XmlConvert.ToBoolean(e.Attribute("Update")!.Value))
                .Select(e => e.Name.LocalName)
                .ToList();
            FileUpdater.UpdatedModules.UnionWith(updatedModules);
            FileUpdater.UpdateRange(files);

            // Аналогичная процедура с загружаемыми сборками
            List<string>? loadedModules = StartUpConfig
                .Root?
                .Element("Modules")?
                .Elements()
                .Where(e => XmlConvert.ToBoolean(e.Attribute("Include")!.Value))
                .Select(e => e.Name.LocalName)
                .ToList();
            StructureComparer.IncludedModules.UnionWith(loadedModules);
            List<FileInfo> loadingAssemblies = files.Where(cf => cf.LocalFile.Extension == ".dll" && StructureComparer.IncludedModules.Contains(cf.ModuleTag)).Select(cf => cf.LocalFile).ToList();
            foreach (FileInfo assembly in loadingAssemblies)
            {
                Assembly.LoadFrom(assembly.FullName);
                LoggingRouter.WriteLog.Invoke($"Сборка {assembly.Name} загружена");
            }
            LoggingRouter.WriteLog = null;

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddXmlFile(PathProvider.GetPath(ConfigurationXmlFileName));
            IConfiguration config = configurationBuilder.Build();

            Services.AddSingleton(config)
                    .AddSingleton<ILogger, NcetSimpleLogger>();

            ServiceProvider = Services.BuildServiceProvider();
        }

        public static void InitializeAsLibrary()
        {
            _ = InitializeFileStructure(false);
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddXmlFile(PathProvider.GetPath(ConfigurationXmlFileName));
            IConfiguration config = configurationBuilder.Build();

            Services.AddSingleton(config)
                    .AddSingleton<ILogger, NcetSimpleLogger>();

            ServiceProvider = Services.BuildServiceProvider();
        }

        private static List<ComparedFiles> InitializeFileStructure(bool preUpdate = true)
        {
            // Получаем директорию выполняемой сборки и xml файл со структурой папок приложения
            DirectoryInfo dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
            string structurePath = Path.Combine(dir.FullName, StructureXmlName);
            XDocument structureXml = XDocument.Load(structurePath);

            // Если локальный путь пуст (у только что скачанного xml со структурой), прописать его
            XElement xmlLocalPath = structureXml.Root!.Element("basepath")!.Element("local")!;
            if (xmlLocalPath.Value == string.Empty || xmlLocalPath.IsEmpty)
            {
                xmlLocalPath.Add(dir.Parent!.FullName);
                structureXml.Save(structurePath);
            }

            // Получить наборы файлов для сопоставления (локальный, источник и тег модуля) и создать словарь путей для обращения
            List<ComparedFiles> files = StructureComparer.GetFiles(structureXml);
            PathProvider.InitializeStructure(dir.Parent!.FullName);

            if (preUpdate)
            {
                // Сразу обновляем список изменений и инструкцию со списком команд
                ComparedFiles cfiles = files.Where(f => f.LocalFile.FullName == PathProvider.GetPath(StartUpConfigName)).FirstOrDefault();
                FileUpdater.UpdateFile(cfiles.LocalFile, cfiles.SourceFile);
                cfiles = files.Where(f => f.LocalFile.FullName == PathProvider.GetPath("Список изменений.txt")).FirstOrDefault();
                FileUpdater.UpdateFile(cfiles.LocalFile, cfiles.SourceFile);
                cfiles = files.Where(f => f.LocalFile.FullName == PathProvider.GetPath("Команды.txt")).FirstOrDefault();
                FileUpdater.UpdateFile(cfiles.LocalFile, cfiles.SourceFile);
            }
            return files;


        }
        private static Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                string filename = string.Concat(args.Name.Split(", ")[0], ".dll");
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

        private static IEnumerable<FileInfo>? SearchDirectoryForDlls(DirectoryInfo directory)
        {
            return directory.GetFiles("*", SearchOption.AllDirectories).Where(fi => fi.Extension == ".dll");

        }



        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        [CommandMethod("NCET_CONFIG")]
        public static void ConfigureAutorun()
        {
            StartUpWindow window = new(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
            window.bUpdateLayerWorks.IsEnabled = false;
            window.bUpdateUtilities.IsEnabled = false;
            window.bUpdateGeoMod.IsEnabled = false;
            Application.ShowModalWindow(window);
        }
    }
}
