using LoaderCore.Integrity;
using System.Collections.Generic;
using System.Linq;

namespace LoaderCore.Utilities
{
    public static class PathProvider
    {
        private static Dictionary<string, string> PathDictionary { get; set; } = null!;

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

        public static bool TryGetPath(string name, out string? path)
        {
            return PathDictionary.TryGetValue(name, out path);
        }
    }
}
