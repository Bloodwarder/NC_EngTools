using System;
using System.IO;
using System.Xml.Linq;
using System.Reflection;
using Teigha.Runtime;
using HostMgd.ApplicationServices;

namespace NC_EngTools_Loader
{
    public class StartUp : IExtensionApplication
    {
        const string DefaultSourceDirectory = @"\\Comp575\обмен - коновалов\NC_EngTools";
        const string LoaderCoreDirectory = "ExtensionLibraries";
        const string LoaderCoreAssemblyName = "LoaderCore.dll";
        const string StructureXmlName = "Structure.xml";

        private readonly FileInfo LocalStartupAssemblyFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
        private string sourceDirectory;
        private string SourceDirectory
        {
            get
            {
                if (sourceDirectory == null)
                {
                    try
                    {
                        XDocument xDocument = XDocument.Load(Path.Combine(LocalStartupAssemblyFile.DirectoryName,StructureXmlName));
                        sourceDirectory = xDocument.Root.Element("basepath").Element("source").Value;
                        xDocument = null;
                        GC.Collect();
                    }
                    catch (System.Exception)
                    {
                        sourceDirectory = DefaultSourceDirectory;
                    }
                }
                return sourceDirectory;
            }
            set => sourceDirectory = value;
        }

        /// <summary>
        /// Поиск и загрузка сборки автозагрузчика и xml файла структуры при старте nanoCAD
        /// </summary>
        public void Initialize()
        {
            FileInfo localLoaderAssemblyFile = new FileInfo(Path.Combine(LocalStartupAssemblyFile.DirectoryName, LoaderCoreDirectory, LoaderCoreAssemblyName));
            FileInfo sourceLoaderAssemblyFile = new FileInfo(Path.Combine(SourceDirectory, LoaderCoreDirectory, LoaderCoreAssemblyName));
            FileInfo localStructureXml = new FileInfo(Path.Combine(LocalStartupAssemblyFile.DirectoryName, LoaderCoreDirectory, StructureXmlName));
            FileInfo sourceStructureXml = new FileInfo(Path.Combine(SourceDirectory, LoaderCoreDirectory, StructureXmlName));
            bool xmlupdated;
            try
            {
                UpdateFile(localLoaderAssemblyFile, sourceLoaderAssemblyFile, out _);
                UpdateFile(localStructureXml, sourceStructureXml, out xmlupdated);
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.Message);
                return;
            }
            if (xmlupdated)
            {
                try
                {
                    XDocument xmldoc = XDocument.Load(localStructureXml.FullName);
                    XElement localPathElement = xmldoc.Root.Element("basepath").Element("local");
                    localPathElement.Value = LocalStartupAssemblyFile.Directory.FullName;
                    xmldoc.Save(localStructureXml.FullName);
                }
                catch (System.Exception)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Ошибка работы с файлом {StructureXmlName}");
                    return;
                }
            }
            Assembly loaderAssembly = Assembly.LoadFrom(localLoaderAssemblyFile.FullName);
            Type loaderType = loaderAssembly.GetType("LoaderExtension");
            MethodInfo initializeMethod = loaderType.GetMethod("Initialize");
            initializeMethod.Invoke(null, null);
        }

        /// <summary>
        /// Код, выполняемый при завершении nanoCAD (на данный момент отсутствует)
        /// </summary>
        public void Terminate()
        {
        }

        private void UpdateFile(FileInfo local, FileInfo source, out bool updated)
        {
            bool localExists = local.Exists;
            bool sourceExists = source.Exists;
            if (!localExists && !sourceExists)
            {
                updated = false;
                throw new System.Exception($"\nПриложение не загружено. Отсутствует локальный файл {local.Name} и нет доступа к файлам обновления");
            }
            if (sourceExists && (!localExists || local.LastWriteTime != source.LastWriteTime))
            {
                source.CopyTo(local.FullName, true);
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Файл {local.Name} обновлён");
                updated = true;
                return;
            }
            updated = false;
        }
    }
}
