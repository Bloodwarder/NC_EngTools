namespace ExternalData
{
    using System.Collections.Generic;
    using Microsoft.Office.Interop.Excel;
    using System.IO;
    using System.Xml.Serialization;
    using Teigha.Runtime;

    public class PropsReloader
    {
        //здесь необходимо задать относительный путь и взять его из файла конфигурации. сделаю, как разберусь
        //const string path = "/LayerData/";
        //ConfigurationManager.AppSettings.Get("layerpropsxlsxpath");
        const string xlname = "Layer_Props.xlsm";
        const string xmlpropsname = "Layer_Props.xml";
        const string xmlaltername = "Layer_Alter.xml";
        public static string Path;
        static PropsReloader()
        {
            FileInfo fi = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Path = fi.DirectoryName + "\\LayersData\\";
        }


        [CommandMethod("RELOADPROPS")]
        public void ReloadDictionaries()
        {
            XmlSerializableDictionary<string, LayerProps> dct = ExtractPropsExcel();
            XmlSerializableDictionary<string, string> dct2 = ExtractAlterExcel();
            XmlSerializeProps(dct);
            XmlSerializeAlteringDictionary(dct2);
            LayerProperties.Dictionary = dct;
            LayerAlteringDictionary.Dictionary = dct2;
        }

        private XmlSerializableDictionary<string, LayerProps> ExtractPropsExcel()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };
            FileInfo fi = new FileInfo(Path+xlname);
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

        private XmlSerializableDictionary<string, string> ExtractAlterExcel()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };
            FileInfo fi = new FileInfo(Path+xlname);
            if (!fi.Exists) { throw new System.Exception("Файл не существует"); }

            Workbook xlwb = xlapp.Workbooks.Open(fi.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            XmlSerializableDictionary<string, string> dct = new XmlSerializableDictionary<string, string>();
            try
            {

                Range rng = xlwb.Worksheets[2].Cells[1, 1].CurrentRegion;
                for (int i = 1; i < rng.Rows.Count+1; i++)
                {
                    string strkey = rng.Cells[i, 1].Text;
                    string strval = rng.Cells[i, 2].Text;
                    dct.Add(strkey, strval);
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
            using (FileStream fs = new FileStream(Path+xmlpropsname, FileMode.Create))
            {
                xs.Serialize(fs, dictionary);
            }
        }

        public static Dictionary<string, LayerProps> XmlDeserializeProps()
        {
            FileInfo fi = new FileInfo(Path+xmlpropsname);
            if (!fi.Exists) { throw new System.Exception("Файл не существует"); }
            
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, LayerProps>));
            using (FileStream fs = new FileStream(Path+xmlpropsname, FileMode.Open))
            {
                XmlSerializableDictionary<string, LayerProps> dct = xs.Deserialize(fs) as XmlSerializableDictionary<string, LayerProps>;
                return dct;
            }
        }

        private void XmlSerializeAlteringDictionary(XmlSerializableDictionary<string,string> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, string>));
            using (FileStream fs = new FileStream(Path+xmlaltername, FileMode.Create))
            {
                xs.Serialize(fs, dictionary);
            }
        }

        public static Dictionary<string, string> XmlDeserializeAlteringDictionary()
        {
            FileInfo fi = new FileInfo(Path+xmlaltername);
            if (!fi.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, string>));
            using (FileStream fs = new FileStream(Path+xmlaltername, FileMode.Open))
            {
                XmlSerializableDictionary<string, string> dct = xs.Deserialize(fs) as XmlSerializableDictionary<string, string>;
                return dct;
            }
        }
    }
    public class LayerProperties
    {
        public static Dictionary<string, LayerProps> Dictionary{ get; set; }
        static LayerProperties()
        {
            try
            {
                Dictionary = PropsReloader.XmlDeserializeProps();
            }
            catch (FileNotFoundException)
            {
                PropsReloader pr = new PropsReloader();
                pr.ReloadDictionaries();
            }
        }
    }

    public static class LayerAlteringDictionary
    {
        public static Dictionary<string, string> Dictionary { get; set; }
        static LayerAlteringDictionary() 
        {
            try
            {
                Dictionary = PropsReloader.XmlDeserializeAlteringDictionary();
            }
            catch(FileNotFoundException)
            {
                PropsReloader pr = new PropsReloader();
                pr.ReloadDictionaries();
            }
        }
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