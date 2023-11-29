using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LayerWorks.ExternalData
{
    internal static class PathOrganizer
    {
        //здесь необходимо задать относительный путь и взять его из файла конфигурации. сделаю, как разберусь
        //const string path = "/LayerData/";
        //ConfigurationManager.AppSettings.Get("layerpropsxlsxpath");
        const string xlname = "Layer_Props.xlsm";
        const string xmlpropsname = "Layer_Props.xml";
        const string xmlaltername = "Layer_Alter.xml";
        const string xmllegendname = "Layer_Legend.xml";
        const string xmllegenddrawaname = "Layer_LegendDraw.xml";
        const string linname = "STANDARD1.lin";
        private readonly static Dictionary<string, string> _pathDictionary = new Dictionary<string, string>();
        public static string BasePath { get; private set; }
        static PathOrganizer()
        {
            FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string BasePath = fi.DirectoryName + "\\LayersData\\";

            _pathDictionary.Add("Excel", string.Concat(BasePath, xlname));
            _pathDictionary.Add("Props", string.Concat(BasePath, xmlpropsname));
            _pathDictionary.Add("Alter", string.Concat(BasePath, xmlaltername));
            _pathDictionary.Add("Linetypes", string.Concat(BasePath, linname));
            _pathDictionary.Add("Legend", string.Concat(BasePath, xmllegendname));
            _pathDictionary.Add("LegendDraw", string.Concat(BasePath, xmllegenddrawaname));
        }

        public static string GetPath(string sourcename)
        {
            return _pathDictionary[sourcename];
        }
    }
}