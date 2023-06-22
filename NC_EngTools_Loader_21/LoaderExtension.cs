using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Teigha.Runtime;

namespace Loader
{
    public class LoaderExtension : IExtensionApplication
    {
        public void Initialize()
        {
            ToolsAssemblyDirectory sourcePackage = new ToolsAssemblyDirectory(@"\\Comp575\обмен - коновалов\NC_EngTools");
            FileInfo assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
            ToolsAssemblyDirectory localPackage = new ToolsAssemblyDirectory(assembly.DirectoryName);
            localPackage.CompareAndRewrite(sourcePackage);
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
    }

    internal class ToolsAssemblyDirectory
    {
        private const string ToolsAssemblyName = "NC_EngTools_21.dll";
        private readonly FileInfo _toolsAssemblyInfo;
        private readonly List<FileInfo> _xmlDataFiles = new List<FileInfo>();
        private readonly DirectoryInfo _directory;
        private readonly DirectoryInfo _layersDataDirectory;

        internal FileInfo ToolsAssemblyInfo => _toolsAssemblyInfo;
        internal List<FileInfo> XmlDataFiles => _xmlDataFiles;
        internal ToolsAssemblyDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            _toolsAssemblyInfo = new FileInfo($"{_directory.FullName}\\{ToolsAssemblyName}");
            _layersDataDirectory = new DirectoryInfo($"{_directory.FullName}\\LayersData");
            if (!_layersDataDirectory.Exists)
                _layersDataDirectory.Create();
            _xmlDataFiles.AddRange(_layersDataDirectory.GetFiles().Where(f => f.Extension == ".xml").ToList());
        }
        internal ToolsAssemblyDirectory(string path) : this(new DirectoryInfo(path)) { }


        public void CompareAndRewrite(ToolsAssemblyDirectory sourceDirectory)
        {
            foreach (FileInfo file in sourceDirectory.XmlDataFiles) 
            {
                FileInfo localFile = new FileInfo($"{_layersDataDirectory.FullName}\\{file.Name}");
                if (!localFile.Exists || file.LastWriteTime > localFile.LastWriteTime)
                {
                    try
                    {
                        file.CopyTo(localFile.FullName, true);
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                }
            }
            if (!_toolsAssemblyInfo.Exists || sourceDirectory.ToolsAssemblyInfo.LastWriteTime > _toolsAssemblyInfo.LastWriteTime)
                sourceDirectory.ToolsAssemblyInfo.CopyTo(_toolsAssemblyInfo.FullName);
        }
    }
}
