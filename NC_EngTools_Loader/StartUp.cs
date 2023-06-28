using HostMgd.EditorInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Teigha.Runtime;
using HostMgd.ApplicationServices;

namespace NC_EngTools_Loader
{
    public class StartUp : IExtensionApplication
    {
        const string SourceDirectory = @"\\Comp575\обмен - коновалов\NC_EngTools";
        const string LoaderCoreDirectory = "ExtensionLibraries";
        const string LoaderCoreAssemblyName = "LoaderCore.dll";

        public void Initialize()
        {
            FileInfo localStartupAssemblyFile = new FileInfo(Assembly.GetExecutingAssembly().Location);
            FileInfo localLoaderAssemblyFile = new FileInfo(Path.Combine(localStartupAssemblyFile.DirectoryName, LoaderCoreDirectory, LoaderCoreAssemblyName));
            FileInfo sourceLoaderAssemblyFile = new FileInfo(Path.Combine(SourceDirectory, LoaderCoreDirectory, LoaderCoreAssemblyName));
            if (!localLoaderAssemblyFile.Exists && !sourceLoaderAssemblyFile.Exists)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Приложение не загружено. Отсутствует локальный файл {LoaderCoreAssemblyName} и нет доступа к файлам обновления");
                return;
            }
            if (!localStartupAssemblyFile.Exists || localLoaderAssemblyFile.LastWriteTime != sourceLoaderAssemblyFile.LastWriteTime)
            {
                sourceLoaderAssemblyFile.CopyTo(localLoaderAssemblyFile.FullName, true);
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"Файл {localLoaderAssemblyFile.Name} обновлён");
            }
            Assembly loaderAssembly = Assembly.LoadFrom(localLoaderAssemblyFile.FullName);
            Type loaderType = loaderAssembly.GetType("LoaderExtension");
            MethodInfo initializeMethod = loaderType.GetMethod("Initialize");
            initializeMethod.Invoke(null, null);
        }

        public void Terminate()
        {
        }
    }
}
