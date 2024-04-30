using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LoaderCore.Integrity
{
    internal static class StructureComparer
    {
        /// <summary>
        /// Сет с тегами загружаемых модулей
        /// </summary>
        internal static HashSet<string> IncludedModules { get; set; } = new HashSet<string>() { "General" };

        /// <summary>
        /// Получить все пути файлов в локальной папке, папке источнике и теги модулей
        /// </summary>
        /// <param name="xDocument">xml-документ со структурой папок приложения</param>
        /// <returns>Список сравниваемых файлов</returns>
        internal static List<ComparedFiles> GetFiles(XDocument xDocument)
        {
            XElement innerpath = xDocument.Root.Element("innerpath");
            string localPath = xDocument.Root.Element("basepath").Element("local").Value;
            string sourcePath = xDocument.Root.Element("basepath").Element("source").Value;

            List<ComparedFiles> result = new();
            result.AddRange(SearchStructure(innerpath, localPath, sourcePath));
            return result;
        }
        /// <summary>
        /// Рекурсивная функция поиска по структуре xml-файла
        /// </summary>
        /// <param name="element">Элемент innerpath в файле структуры</param>
        /// <param name="localPath">Полный путь к локальной папке</param>
        /// <param name="sourcePath">Полный путь к папке-источнику</param>
        /// <returns></returns>
        private static IEnumerable<ComparedFiles> SearchStructure(XElement element, string localPath, string sourcePath)
        {
            if (!element.HasElements)
                return Enumerable.Empty<ComparedFiles>();
            List<ComparedFiles> results = new();
            foreach (XElement childElement in element.Elements("directory"))
            {
                string directoryPath = Path.Combine(localPath, childElement.Attribute("Name").Value);
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                results.AddRange(SearchStructure(
                    childElement,
                    Path.Combine(localPath, childElement.Attribute("Name").Value),
                    Path.Combine(sourcePath, childElement.Attribute("Name").Value)));
            }
            foreach (XElement childElement in element.Elements("file"))
            {
                //if (!IncludedModules.Contains(childElement.Attribute("Module").Value))
                //    continue;
                ComparedFiles compared = new                    (
                    new FileInfo(Path.Combine(localPath, childElement.Attribute("Name").Value)),
                    new FileInfo(Path.Combine(sourcePath, childElement.Attribute("Name").Value)),
                    childElement.Attribute("Module").Value
                    );
                new FileInfo(Path.Combine(localPath, childElement.Attribute("Name").Value));
                results.Add(compared);
            }
            return results;
        }
    }
}
