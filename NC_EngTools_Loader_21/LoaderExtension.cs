using HostMgd.ApplicationServices;
using LoaderUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Teigha.Runtime;

namespace Loader
{
    /// <summary>
    /// Загрузчик сборок с окном конфигурации
    /// </summary>
    public static class LoaderExtension
    {
        const string StructureXmlName = "Structure.xml";
        const string StartUpConfigName = "StartUpConfig.xml";

        // Поиск обновлений и настройка загрузки и обновления сборок
        public static void Initialize()
        {
            Logger.WriteLog += Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage;
            // Получаем директорию выполняемой сборки и xml файл со структурой папок приложения
            DirectoryInfo dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            XDocument structureXml = XDocument.Load(Path.Combine(dir.FullName, StructureXmlName));
            // Получить наборы файлов для сопоставления (локальный, источник и тег модуля) и создать словарь путей для обращения
            List<ComparedFiles> files = StructureComparer.GetFiles(structureXml);
            PathProvider.InitializeStructure(files);
            // Читаем конфигурацию на предмет необходимости отображения стартового окна и отображаем его
            XDocument StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));
            bool showStartUp = XmlConvert.ToBoolean(StartUpConfig.Root.Element("StartUpShow").Attribute("Enabled").Value);
            if (showStartUp)
            {
                StartUpWindow window = new StartUpWindow(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
                window.ShowDialog();
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

        /// <summary>
        /// Команда вызова окна конфигурации
        /// </summary>
        [CommandMethod("NCET_CONFIG")]
        public static void ConfigureAutorun()
        {
            StartUpWindow window = new StartUpWindow(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
            window.ShowDialog();
        }
    }

    public static class PathProvider
    {
        private static Dictionary<string, string> PathDictionary { get; set; }

        internal static void InitializeStructure(IEnumerable<ComparedFiles> files)
        {
            PathDictionary = files.ToDictionary(cf => cf.LocalFile.Name, cf => cf.LocalFile.FullName);
        }
        /// <summary>
        /// Получить полный путь файла в директории приложения
        /// </summary>
        /// <param name="name">Имя файла</param>
        /// <returns>Полный путь к файлу</returns>
        public static string GetPath(string name)
        {
            return PathDictionary[name];
        }
    }

    internal static class StructureComparer
    {
        /// <summary>
        /// Сет с тегами загружаемых модулей
        /// </summary>
        internal static HashSet<string> IncludedModules { get; set; } = new HashSet<string>() { "General" };

        /// <summary>
        /// Получить все пути файлов в локальной папке, папке источнике и теги модулей
        /// </summary>
        /// <param name="xDocument">xml-документ со структурой папок приложения</param>
        /// <returns>Список сравниваемых файлов</returns>
        internal static List<ComparedFiles> GetFiles(XDocument xDocument)
        {
            XElement innerpath = xDocument.Root.Element("innerpath");
            string localPath = xDocument.Root.Element("basepath").Element("local").Value;
            string sourcePath = xDocument.Root.Element("basepath").Element("source").Value;

            List<ComparedFiles> result = new List<ComparedFiles>();
            result.AddRange(SearchStructure(innerpath, localPath, sourcePath));
            return result;
        }
        /// <summary>
        /// Рекурсивная функция поиска по структуре xml-файла
        /// </summary>
        /// <param name="element">Элемент innerpath в файле структуры</param>
        /// <param name="localPath">Полный путь к локальной папке</param>
        /// <param name="sourcePath">Полный путь к папке-источнику</param>
        /// <returns></returns>
        private static IEnumerable<ComparedFiles> SearchStructure(XElement element, string localPath, string sourcePath)
        {
            if (!element.HasElements)
                return Enumerable.Empty<ComparedFiles>();
            List<ComparedFiles> results = new List<ComparedFiles>();
            foreach (XElement childElement in element.Elements("directory"))
            {
                results.AddRange(SearchStructure(
                    childElement,
                    Path.Combine(localPath, childElement.Attribute("Name").Value),
                    Path.Combine(sourcePath, childElement.Attribute("Name").Value)));
            }
            foreach (XElement childElement in element.Elements("file"))
            {
                //if (!IncludedModules.Contains(childElement.Attribute("Module").Value))
                //    continue;
                ComparedFiles compared = new ComparedFiles
                    (
                    new FileInfo(Path.Combine(localPath, childElement.Attribute("Name").Value)),
                    new FileInfo(Path.Combine(sourcePath, childElement.Attribute("Name").Value)),
                    childElement.Attribute("Module").Value
                    );
                new FileInfo(Path.Combine(localPath, childElement.Attribute("Name").Value));
                results.Add(compared);
            }
            return results;
        }
    }

    internal static class FileUpdater
    {
        /// <summary>
        /// Сет с тегами обновляемых модулей
        /// </summary>
        internal static HashSet<string> UpdatedModules { get; } = new HashSet<string>() { "General" };

        internal static event EventHandler FileUpdatedEvent;

        /// <summary>
        /// Выключить для реального обновления файлов
        /// </summary>
        private static readonly bool _testRun = Assembly.GetExecutingAssembly().GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        internal static void UpdateFile(FileInfo local, FileInfo source)
        {
            bool localExists = local.Exists;
            bool sourceExists = source.Exists;
            // Если нет ни локального файла, ни источника, выдаем ошибку
            if (!localExists && !sourceExists)
            {
                throw new System.Exception($"\nОтсутствует локальный файл {local.Name} и нет доступа к файлам обновления");
            }
            // Если доступен источник, сравниваем даты обновления и при необходимости перезаписываем локальный файл. Если нет - работаем с локальным без обновления
            if (sourceExists && (!localExists || local.LastWriteTime != source.LastWriteTime))
            {
                if (_testRun)
                {
                    Logger.WriteLog?.Invoke($"Отладочная сборка. Вывод сообщения об обновлении {local.Name}");
                    return;
                }
                source.CopyTo(local.FullName, true);
                FileUpdatedEvent?.Invoke(local, new EventArgs());
                Logger.WriteLog?.Invoke($"Файл {local.Name} обновлён");
                return;
            }
        }

        internal static void UpdateFile(ComparedFiles comparedFiles)
        {
            if (UpdatedModules.Contains(comparedFiles.ModuleTag))
                UpdateFile(comparedFiles.LocalFile, comparedFiles.SourceFile);
        }

        internal static void UpdateRange(IEnumerable<ComparedFiles> comparedFiles, string singleTagUpdate = null)
        {
            // Если не задан конкретный тег - заранее фильтрует набор по нему и обновляет с пропуском стандартной проверки
            // Если задан - сразу передаёт в метод со стандартной проверкой на содержание тега в наборе обновляемых модулей
            if (singleTagUpdate == null)
            {
                foreach (ComparedFiles fileSet in comparedFiles)
                {
                    if (!fileSet.LocalFile.Directory.Exists)
                        fileSet.LocalFile.Directory.Create();
                    UpdateFile(fileSet);
                }
            }
            else
            {
                foreach (ComparedFiles fileSet in comparedFiles.Where(fs => fs.ModuleTag == singleTagUpdate).ToList())
                    UpdateFile(fileSet.LocalFile, fileSet.SourceFile);
            }
        }

    }

    internal static class Logger
    {
        internal static Log WriteLog;

        internal delegate void Log(string message);
    }

    internal struct ComparedFiles
    {
        internal FileInfo LocalFile;
        internal FileInfo SourceFile;
        internal string ModuleTag;

        internal ComparedFiles(FileInfo local, FileInfo source, string moduleTag)
        {
            LocalFile = local;
            SourceFile = source;
            ModuleTag = moduleTag;
        }
    }
}
