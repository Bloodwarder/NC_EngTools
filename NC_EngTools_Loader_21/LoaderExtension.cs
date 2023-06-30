using HostMgd.ApplicationServices;
using HostMgd.Windows;
using LoaderUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Teigha.Runtime;

namespace Loader
{

    public static class LoaderExtension
    {
        const string StructureXmlName = "Structure.xml";
        const string StartUpConfigName = "StartUpConfig.xml";

        public static void Initialize()
        {
            //System.Windows.Window newWindow = new LoaderUI.StartUpWindow();
            //newWindow.ShowDialog();
            DirectoryInfo dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            XDocument structureXml = XDocument.Load(Path.Combine(dir.FullName, StructureXmlName));
            List<ComparedFiles> files = StructureComparer.GetFiles(structureXml);
            PathProvider.InitializeStructure(files);
            StartUpWindow window = new StartUpWindow(PathProvider.GetPath(StartUpConfigName), PathProvider.GetPath(StructureXmlName));
            window.ShowDialog();
            XDocument StartUpConfig = XDocument.Load(PathProvider.GetPath(StartUpConfigName));
            List<string> updatedModules = StartUpConfig
                .Root
                .Element("Modules")
                .Elements()
                .Where(e => XmlConvert.ToBoolean(e.Attribute("Update").Value))
                .Select(e => e.Name.LocalName)
                .ToList();
            FileUpdater.UpdatedModules.UnionWith(updatedModules);
            FileUpdater.UpdateRange(files);
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
                Assembly.LoadFrom(assembly.FullName);
        }
    }

    public static class PathProvider
    {
        private static Dictionary<string, string> PathDictionary { get; set; }

        internal static void InitializeStructure(IEnumerable<ComparedFiles> files)
        {
            PathDictionary = files.ToDictionary(cf => cf.LocalFile.Name, cf => cf.LocalFile.FullName);
        }

        public static string GetPath(string name)
        {
            return PathDictionary[name];
        }
    }

    internal static class StructureComparer
    {
        internal static HashSet<string> IncludedModules { get; set; } = new HashSet<string>() { "General" };
        internal static List<ComparedFiles> GetFiles(XDocument xDocument)
        {
            XElement innerpath = xDocument.Root.Element("innerpath");
            string localPath = xDocument.Root.Element("basepath").Element("local").Value;
            string sourcePath = xDocument.Root.Element("basepath").Element("source").Value;

            List<ComparedFiles> result = new List<ComparedFiles>();
            result.AddRange(SearchStructure(innerpath, localPath, sourcePath));
            return result;
        }

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
        internal static HashSet<string> UpdatedModules { get; } = new HashSet<string>() { "General" };

        internal static event EventHandler FileUpdated;

        private static readonly bool _testRun = true;
        internal static void UpdateFile(FileInfo local, FileInfo source)
        {
            bool localExists = local.Exists;
            bool sourceExists = source.Exists;
            if (!localExists && !sourceExists)
            {
                throw new System.Exception($"\nОтсутствует локальный файл {local.Name} и нет доступа к файлам обновления");
            }
            if (sourceExists && (!localExists || local.LastWriteTime != source.LastWriteTime))
            {
                if (!_testRun)
                    source.CopyTo(local.FullName, true);
                FileUpdated(local, new EventArgs());
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Файл {local.Name} обновлён");
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
            List<ComparedFiles> workSet = singleTagUpdate != null ? comparedFiles.ToList() : comparedFiles.Where(cf => cf.ModuleTag == singleTagUpdate).ToList();

            foreach (ComparedFiles fileSet in workSet)
                UpdateFile(fileSet.LocalFile, fileSet.SourceFile);
        }

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
