using HostMgd.ApplicationServices;
using LoaderCore.Utilities;
using LoaderCore.Integrity;
using LoaderCore.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Teigha.Runtime;
using System;

namespace LoaderCore
{
    /// <summary>
    /// Загрузчик сборок с окном конфигурации
    /// </summary>
    public static class LoaderExtension
    {
        const string StructureXmlName = "Structure.xml";
        const string StartUpConfigName = "StartUpConfig.xml";

        static Dictionary<string, string>? LibraryFiles { get; } = new();
        // Поиск обновлений и настройка загрузки и обновления сборок
        static LoaderExtension()
        {
            DirectoryInfo dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.Parent;
            IEnumerable<FileInfo> files = SearchDirectoryForDlls(dir);
            foreach (FileInfo fi in files)
            {
                LibraryFiles[fi.Name] = fi.FullName;
            }
        }

        
        public static void Initialize()
        {

            Logger.WriteLog += Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage;

            List<ComparedFiles> files = InitializeFileStructure();

            // Читаем конфигурацию на предмет необходимости отображения стартового окна и отображаем его
            XDocument StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));
            bool showStartUp = XmlConvert.ToBoolean(StartUpConfig.Root.Element("StartUpShow").Attribute("Enabled").Value);
            if (showStartUp)
            {
                StartUpWindow window = new StartUpWindow(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
                Application.ShowModalWindow(window);
            }

            // Заново читаем конфиг (мог измениться)
            StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));

            // Получаем из конфига теги обновляемых модулей и обновляем все необходимые файлы, помеченные указанными тегами
            List<string> updatedModules = StartUpConfig
                .Root
                .Element("Modules")
                .Elements()
                .Where(e => XmlConvert.ToBoolean(e.Attribute("Update").Value))
                .Select(e => e.Name.LocalName)
                .ToList();
            FileUpdater.UpdatedModules.UnionWith(updatedModules);
            FileUpdater.UpdateRange(files);

            // Аналогичная процедура с загружаемыми сборками
            List<string> loadedModules = StartUpConfig
                .Root
                .Element("Modules")
                .Elements()
                .Where(e => XmlConvert.ToBoolean(e.Attribute("Include").Value))
                .Select(e => e.Name.LocalName)
                .ToList();
            StructureComparer.IncludedModules.UnionWith(loadedModules);
            List<FileInfo> loadingAssemblies = files.Where(cf => cf.LocalFile.Extension == ".dll" && StructureComparer.IncludedModules.Contains(cf.ModuleTag)).Select(cf => cf.LocalFile).ToList();
            foreach (FileInfo assembly in loadingAssemblies)
            {
                Assembly.LoadFrom(assembly.FullName);
                Logger.WriteLog.Invoke($"Сборка {assembly.Name} загружена");
            }
            Logger.WriteLog = null;
        }

        public static void InitializeAsLibrary()
        {
            _ = InitializeFileStructure(false);
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
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
            PathProvider.InitializeStructure(files);

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
                    return Assembly.LoadFrom(assemblyPath);
            }
            catch
            {
                return null;
            }
            return null;
        }

        private static IEnumerable<FileInfo>? SearchDirectoryForDlls(DirectoryInfo directory)
        {
            List<FileInfo> fileList = directory.GetFiles().Where(fi => fi.Extension == ".dll").ToList();
            DirectoryInfo[] dirList = directory.GetDirectories();
            foreach (DirectoryInfo dir in dirList)
            {
                IEnumerable<FileInfo>? fi = SearchDirectoryForDlls(dir);
                if (fi.Any())
                    fileList.AddRange(fi);
            }
            return fileList;
        }



        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        [CommandMethod("NCET_CONFIG")]
        public static void ConfigureAutorun()
        {
            StartUpWindow window = new StartUpWindow(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
            window.bUpdateLayerWorks.IsEnabled = false;
            window.bUpdateUtilities.IsEnabled = false;
            window.bUpdateGeoMod.IsEnabled = false;
            Application.ShowModalWindow(window);
        }
    }
}
