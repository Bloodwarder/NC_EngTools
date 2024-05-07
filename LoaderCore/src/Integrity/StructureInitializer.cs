using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoaderCore.Integrity
{
    internal static class StructureInitializer
    {
        private static Dictionary<string, FileInfo> _fileStructure = null!;

        internal static void Initialize(string baseDirectory)
        {
            _fileStructure = GetProjectFiles(baseDirectory);
        }

        public static string GetFile(string fileName)
        {
            return _fileStructure[fileName].FullName;
        }

        public static bool TryGetFile(string fileName, out string? filePath)
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
