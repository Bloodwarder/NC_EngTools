using LoaderCore.Integrity;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace LoaderCore.Utilities
{
    public static class PathProvider
    {
        private static Dictionary<string, FileInfo> _fileStructure = null!;

        internal static void InitializeStructure(string baseDirectory)
        {
            _fileStructure = GetProjectFiles(baseDirectory);
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

        internal static Dictionary<string, FileInfo> GetProjectFiles(DirectoryInfo directory)
        {
            return new(GetFilesFromDirectory(directory));
        }

        internal static Dictionary<string, FileInfo> GetProjectFiles(string directoryPath)
        {
            DirectoryInfo directory = new(directoryPath);
            if (directory.Exists)
            {
                return GetProjectFiles(directory);
            }
            else
            {
                throw new Exception("Directory don't exist");
            }
        }

        private static IEnumerable<KeyValuePair<string, FileInfo>> GetFilesFromDirectory(DirectoryInfo directory)
        {
            var files = directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo file in files)
                yield return new(file.Name, file);
        }
    }
}
