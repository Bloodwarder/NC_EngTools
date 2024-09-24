using HostMgd.ApplicationServices;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Teigha.Runtime;

namespace StartUp
{
    public class StartUp : IExtensionApplication
    {
        const string DefaultSourceDirectory = @"\\Comp575\обмен - коновалов\NC_EngTools";
        const string LoaderCoreDirectory = @"ExtensionLibraries\LoaderCore";
        const string LoaderCoreAssemblyName = "LoaderCore.dll";
        const string ConfigurationXmlName = "Configuration.xml";

        private readonly bool _debugAssembly = Assembly.GetExecutingAssembly()
                                                       .GetCustomAttributes(false)
                                                       .OfType<DebuggableAttribute>()
                                                       .Any(da => da.IsJITTrackingEnabled);
        private readonly FileInfo LocalStartUpAssemblyFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
        private string _sourceDirectory;
        private string SourceDirectory
        {
            get
            {
                if (_sourceDirectory == null)
                {
                    try
                    {
                        XDocument xDocument = XDocument.Load(Path.Combine(LocalStartUpAssemblyFile.DirectoryName,
                                                                          LoaderCoreDirectory,
                                                                          ConfigurationXmlName));
                        _sourceDirectory = xDocument.Root.Element("Directories").Element("UpdateDirectory").Value;
                    }
                    catch (System.Exception)
                    {
                        _sourceDirectory = DefaultSourceDirectory;
                    }
                }
                return _sourceDirectory;
            }
            set => _sourceDirectory = value;
        }

        /// <summary>
        /// Поиск и загрузка сборки автозагрузчика и xml файла структуры при старте nanoCAD
        /// </summary>
        public void Initialize()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Загрузка NC_EngTools");
            FileInfo localLoaderAssemblyFile = new FileInfo(Path.Combine(LocalStartUpAssemblyFile.DirectoryName,
                                                                         LoaderCoreDirectory,
                                                                         LoaderCoreAssemblyName));
            FileInfo sourceLoaderAssemblyFile = new FileInfo(Path.Combine(SourceDirectory,
                                                                          LoaderCoreDirectory,
                                                                          LoaderCoreAssemblyName));
            FileInfo localConfigurationXml = new FileInfo(Path.Combine(LocalStartUpAssemblyFile.DirectoryName,
                                                                   LoaderCoreDirectory,
                                                                   ConfigurationXmlName));
            FileInfo sourceConfigurationXml = new FileInfo(Path.Combine(SourceDirectory,
                                                                    LoaderCoreDirectory,
                                                                    ConfigurationXmlName));
            try
            {
                UpdateFile(localLoaderAssemblyFile, sourceLoaderAssemblyFile, out _);
                //UpdateFile(localConfigurationXml, sourceConfigurationXml, out bool xmlupdated);
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.Message);
                return;
            }

            try
            {
                XDocument configXml = XDocument.Load(localConfigurationXml.FullName);
                XElement localPathElement = configXml.Root.Element("Directories").Element("LocalDirectory");
                localPathElement.Value = LocalStartUpAssemblyFile.Directory.FullName;
                configXml.Save(localConfigurationXml.FullName);
            }
            catch (System.Exception)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Ошибка работы с файлом {ConfigurationXmlName}");
                return;
            }

            Assembly loaderAssembly = Assembly.LoadFrom(localLoaderAssemblyFile.FullName);
            Type loaderType = loaderAssembly.GetType("Loader.LoaderExtension", true);
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
            if (!sourceExists)
            {
                updated = false;
                if (localExists)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Отсутствует файл обновлений. Продолжение работы без обновления");
                    return;
                }
                else
                {
                    throw new System.Exception($"\nПриложение не загружено. Отсутствует локальный файл {local.Name} и нет доступа к файлам обновления");
                }
            }
            if (sourceExists && (!localExists || local.LastWriteTime < source.LastWriteTime))
            {
                if (_debugAssembly)
                {
                    updated = false;
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Отладочная сборка. Вывод сообщения об обновлении {local.Name}");
                    return;
                }
                source.CopyTo(local.FullName, true);
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Файл {local.Name} обновлён");
                updated = true;
                return;
            }
            updated = false;
        }
    }
}
