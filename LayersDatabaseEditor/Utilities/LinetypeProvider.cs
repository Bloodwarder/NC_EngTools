using LoaderCore;
using LoaderCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LayersDatabaseEditor.Utilities
{
    internal class LinetypeProvider
    {
        private static IFilePathProvider? _filePathProvider;

        static LinetypeProvider()
        {

            try
            {
                _filePathProvider = NcetCore.ServiceProvider.GetService<IFilePathProvider>();
            }
            catch
            {

            }        
        }

        public static List<string> GetLinetypes()
        {
            List<string> result = new List<string>() { "Continuous" };
            if (_filePathProvider == null)
                return result;
            Regex rgx = new(@"\*(.+),");
            using (StreamReader reader = new(_filePathProvider.GetPath("STANDARD1.lin")))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = rgx.Match(line);
                    if (match.Success)
                        result.Add(match.Groups[1].Value);
                }
            }
            return result;
        }
    }
}
