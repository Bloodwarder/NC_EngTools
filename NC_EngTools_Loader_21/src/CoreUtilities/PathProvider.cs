using Loader.Integrity;
using System.Collections.Generic;
using System.Linq;

namespace Loader.CoreUtilities
{
    public static class PathProvider
    {
        private static Dictionary<string, string> PathDictionary { get; set; }

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
    }
}
