namespace ExternalData
{
    using System.Collections.Generic;
    using Microsoft.Office.Interop.Excel;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Teigha.Runtime;
    using NC_EngTools;
    //using HostMgd.ApplicationServices;
    using HostMgd.EditorInput;
    using Teigha.DatabaseServices;
    using LayerProcessing;

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
            LayerProperties.UpdateDictionary(dct);
            LayerAlteringDictionary.Dictionary = dct2;
        }

        [CommandMethod("EXTRACTLAYERS")]
        public static void ExtractLayersInfoToExcel()
        {
            Application xlapp = new Application
            { DisplayAlerts = false };
            Workbook workbook = xlapp.Workbooks.Add();
            HostMgd.ApplicationServices.Document doc = HostMgd.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            TransactionManager tm = HostApplicationServices.WorkingDatabase.TransactionManager;
            Transaction myT = tm.StartTransaction();
            using (myT)
            {
                LayerTable lt = (LayerTable)tm.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                var layers = (from ObjectId elem in lt
                              let ltr = (LayerTableRecord)tm.GetObject(elem, OpenMode.ForRead)
                              select ltr).ToList();
                int i = 1;
                try
                {
                    foreach (LayerTableRecord ltr in layers)
                    {
                        string checkedname = "";
                        LayerProps lp = new LayerProps();
                        //Попытка распарсить имя слоя для поиска существующих сохранённых свойств
                        try
                        {
                            checkedname = (new SimpleLayerParser(ltr.Name)).TrueName;
                        }
                        catch (WrongLayerException ex)
                        {
                            doc.Editor.WriteMessage(ex.Message);
                        }
                        bool lpsuccess = true;
                        try
                        {
                            lp = LayerProperties.GetLayerProps(checkedname, false);
                        }
                        catch (NoPropertiesException)
                        {
                            lpsuccess = false;
                        }

                        //bool lpsuccess = LayerProperties._dictionary.TryGetValue(checkedname, out LayerProps lp);
                        workbook.Worksheets[1].Cells[i, 1].Value = checkedname != "" ? checkedname : ltr.Name;
                        if (lpsuccess)
                        {
                            workbook.Worksheets[1].Cells[i, 2].Value = lp.ConstWidth;
                            workbook.Worksheets[1].Cells[i, 3].Value = lp.LTScale;
                        }
                        workbook.Worksheets[1].Cells[i, 4].Value = (int)ltr.Color.Red;
                        workbook.Worksheets[1].Cells[i, 5].Value = (int)ltr.Color.Green;
                        workbook.Worksheets[1].Cells[i, 6].Value = (int)ltr.Color.Blue;
                        LinetypeTableRecord lttr = (LinetypeTableRecord)tm.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead);
                        workbook.Worksheets[1].Cells[i, 7].Value = lttr.Name;
                        workbook.Worksheets[1].Cells[i, 8].Value = ltr.LineWeight;
                        i++;
                    }
                }
                finally
                {
                    workbook.SaveAs(Path+"ExtractedLayers.xlsx");
                    workbook.Close();
                    xlapp.Quit();
                }
            }
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
        private void XmlSerializeProps(XmlSerializableDictionary<string, LayerProps> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, LayerProps>));
            using (FileStream fs = new FileStream(Path+xmlpropsname, FileMode.Create))
            {
                xs.Serialize(fs, dictionary);
            }
        }

        internal static Dictionary<string, LayerProps> XmlDeserializeProps()
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

        private void XmlSerializeAlteringDictionary(XmlSerializableDictionary<string, string> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<string, string>));
            using (FileStream fs = new FileStream(Path+xmlaltername, FileMode.Create))
            {
                xs.Serialize(fs, dictionary);
            }
        }

        internal static Dictionary<string, string> XmlDeserializeAlteringDictionary()
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
        private static Dictionary<string, LayerProps> s_dictionary { get; set; }
        private static Dictionary<string, LayerProps> s_defaultLayerProps = new Dictionary<string, LayerProps>();
        static LayerProperties()
        {
            try
            {
                s_dictionary = PropsReloader.XmlDeserializeProps();
                s_defaultLayerProps.Add("сущ", new LayerProps { ConstWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("дем", new LayerProps { ConstWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 107, Green = 107, Blue = 107 });
                s_defaultLayerProps.Add("пр", new LayerProps { ConstWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("неутв", new LayerProps { ConstWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("ндем", new LayerProps { ConstWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 192, Green = 168, Blue = 110 });
                s_defaultLayerProps.Add("нреорг", new LayerProps { ConstWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                PropsReloader pr = new PropsReloader();
                pr.ReloadDictionaries();
            }
        }

        public static LayerProps GetLayerProps(string layername, bool enabledefaults = true)
        {
            SimpleLayerParser slp;
            try
            {
                slp = new SimpleLayerParser(layername);
            }
            catch (WrongLayerException)
            {
                if (enabledefaults)
                {
                    return new LayerProps { ConstWidth=0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3 };
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
            bool success = s_dictionary.TryGetValue(slp.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    return s_defaultLayerProps[slp.BuildStatus];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }
        internal static void UpdateDictionary(Dictionary<string, LayerProps> layerPropertiesDictionary)
        {
            s_dictionary = layerPropertiesDictionary;
        }
    }

    internal class LayerAlteringDictionary
    {
        public static Dictionary<string, string> Dictionary { get; set; }
        static LayerAlteringDictionary()
        {
            try
            {
                Dictionary = PropsReloader.XmlDeserializeAlteringDictionary();
            }
            catch (FileNotFoundException)
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