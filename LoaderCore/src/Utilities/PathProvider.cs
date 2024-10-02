using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace LoaderCore.Utilities
{
    public static class PathProvider
    {
        private static Dictionary<string, FileInfo> _fileStructure = null!;

        internal static void InitializeStructure(string baseDirectory)
        {
            var files = new DirectoryInfo(baseDirectory).GetFiles("*", SearchOption.AllDirectories);
            _fileStructure = new Dictionary<string, FileInfo>();
            foreach (var file in files)
            {
                bool success = _fileStructure.TryAdd(file.Name, file);
                if (!success)
                    NcetCore.Logger.LogDebug($"Обнаружен дублирующийся файл {file.Name}");
            }
        }

        /// <summary>
        /// Получить полный путь файла в директории приложения
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns>Полный путь к файлу</returns>
        public static string GetPath(string fileName)
        {
            return _fileStructure[fileName].FullName;
        }

        public static bool TryGetPath(string fileName, out string? filePath)
        {
            bool success = _fileStructure.TryGetValue(fileName, out FileInfo? file);
            filePath = file?.FullName;
            return success;
        }

        public static FileInfo GetFileInfo(string fileName)
        {
            return _fileStructure[fileName];
        }

        public static bool TryGetFileInfo(string fileName, out FileInfo? file)
        {
            return _fileStructure.TryGetValue(fileName, out file);
        }
    }
}
