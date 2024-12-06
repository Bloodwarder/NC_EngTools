using LoaderCore.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace LoaderCore.Utilities
{
    public class PathProvider : IFilePathProvider
    {
        private Dictionary<string, FileInfo> _fileStructure = null!;

        public PathProvider(IConfiguration configuration)
        {
            var baseDirectory = configuration["Directories:LocalDirectory"];
            InitializeStructure(baseDirectory);
        }
        internal void InitializeStructure(string baseDirectory)
        {
            var files = new DirectoryInfo(baseDirectory).GetFiles("*", SearchOption.AllDirectories);
            _fileStructure = new Dictionary<string, FileInfo>();
            foreach (var file in files)
            {
                bool success = _fileStructure.TryAdd(file.Name, file);
                if (!success)
                    ;//NcetCore.Logger.LogDebug($"Обнаружен дублирующийся файл {file.Name}");
            }
        }

        /// <summary>
        /// Получить полный путь файла в директории приложения
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Полный путь к файлу</returns>
        public string GetPath(string fileName)
        {
            return _fileStructure[fileName].FullName;
        }

        public bool TryGetPath(string fileName, out string? filePath)
        {
            bool success = _fileStructure.TryGetValue(fileName, out FileInfo? file);
            filePath = file?.FullName;
            return success;
        }

        public FileInfo GetFileInfo(string fileName)
        {
            return _fileStructure[fileName];
        }

        public bool TryGetFileInfo(string fileName, out FileInfo? file)
        {
            return _fileStructure.TryGetValue(fileName, out file);
        }

    }
}
