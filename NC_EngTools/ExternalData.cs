namespace LayerPropsExtraction
{
    using System.Collections.Generic;
    using Microsoft.Office.Interop.Excel;
    using System.IO;
    using System.Xml.Serialization;
    using Teigha.Runtime;

    public class PropsReloader
    {
        //здесь необходимо задать относительный путь и взять его из файла конфигурации. сделаю, как разберусь
        const string path = "./LayersData/"; //"C:/Users/ekono/source/repos/NC_EngTools/NC_EngTools/xlLayerData/";
        //const string path = "/LayerData/";
        //ConfigurationManager.AppSettings.Get("layerpropsxlsxpath");
        const string xlname = "Layer_Props.xlsm";
        const string xmlname = "Layer_Props.xml";
        [CommandMethod("RELOADPROPS")]
        public void ReloadProps()
        {
            XmlSerializableDictionary<string, LayerProps> dct = ExtractPropsExcel();
            XmlSerializeProps(dct);
            LayerProperties.Dictionary = dct;
        }

        private XmlSerializableDictionary<string, LayerProps> ExtractPropsExcel()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };
            FileInfo fi = new FileInfo(path+xlname);
            if (!fi.Exists) { throw new System.Exception("Файл не существует"); }

            Workbook xlwb = xlapp.Workbooks.Open(fi.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            XmlSerializableDictionary<string, LayerProps> dct = new XmlSerializableDictionary<string, LayerProps>();
            try
            {
                Range rng = xlwb.Worksheets[1].Cells[1, 1].CurrentRegion;
                for (int i = 1; i < rng.Rows.Count+1; i++)
                {
                    LayerProps lp = new LayerProps
                    {
                        ConstWidth = rng.Cells[i, 2].Value,
                        LTScale = rng.Cells[i, 3].Value,
                        Red = (byte)rng.Cells[i, 4].Value,
                        Green = (byte)rng.Cells[i, 5].Value,
                        Blue = (byte)rng.Cells[i, 6].Value,
                        LTName = rng.Cells[i, 7].Text,
                        LineWeight = (int)rng.Cells[i, 8].Value
                    };
                    string str = rng.Cells[i, 1].Text;
                    dct.Add(str, lp);
                }
            }
            finally
            {
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
            return dct;
        }
        
        private void XmlSerializeProps(XmlSerializableDictionary<string,LayerProps> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string,LayerProps>));
            using (FileStream fs = new FileStream(path+xmlname, FileMode.Create))
            {
                xs.Serialize(fs, dictionary);
            }
        }

        public static Dictionary<string, LayerProps> XmlDeserializeProps()
        {
            FileInfo fi = new FileInfo(path+xmlname);
            if (!fi.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, LayerProps>));
            using (FileStream fs = new FileStream(path+xmlname, FileMode.Open))
            {
                XmlSerializableDictionary<string, LayerProps> dct = xs.Deserialize(fs) as XmlSerializableDictionary<string, LayerProps>;
                return dct; 
            }
        }
    }
    public static class LayerProperties
    {
        public static Dictionary<string, LayerProps> Dictionary { get; set; }

        static LayerProperties() { Dictionary = PropsReloader.XmlDeserializeProps(); }
    }

    public static class LayerAlteringDictionary
    {

    }

    public struct LayerProps
    {
        public double ConstWidth;
        public double LTScale;
        public byte Red;
        public byte Green;
        public byte Blue;
        public string LTName;
        public int LineWeight;
    }
}