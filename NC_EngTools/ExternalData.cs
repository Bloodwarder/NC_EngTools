using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Teigha.Runtime;
using NC_EngTools;
//using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using LayerProcessing;
using System;
using Legend;
using static ModelspaceDraw.BlockReferenceDraw;
using ModelspaceDraw;
using Teigha.DatabaseServices.Filters;

namespace ExternalData
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
        static Dictionary<string, string> pathdictionary = new Dictionary<string, string>();
        public static string BasePath { get; private set; }
        static PathOrganizer()
        {
            FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string BasePath = fi.DirectoryName + "\\LayersData\\";

            pathdictionary.Add("Excel", string.Concat(BasePath, xlname));
            pathdictionary.Add("Props", string.Concat(BasePath, xmlpropsname));
            pathdictionary.Add("Alter", string.Concat(BasePath, xmlaltername));
            pathdictionary.Add("Linetypes", string.Concat(BasePath, linname));
            pathdictionary.Add("Legend", string.Concat(BasePath, xmllegendname));
            pathdictionary.Add("LegendDraw", string.Concat(BasePath, xmllegenddrawaname));
        }

        public static string GetPath(string sourcename)
        {
            return pathdictionary[sourcename];
        }
    }

    internal abstract class DictionaryDataProvider<TKey, TValue>
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }

    internal class XmlDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue>
    {
        string filepath { get; set; }
        FileInfo fileinfo;

        internal XmlDictionaryDataProvider(string path)
        {
            filepath = path;
            fileinfo = new FileInfo(filepath);
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            if (!fileinfo.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(filepath, FileMode.Open))
            {
                XmlSerializableDictionary<TKey, TValue> dct = xs.Deserialize(fs) as XmlSerializableDictionary<TKey, TValue>;
                return dct;
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(filepath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }
    }

    abstract internal class ExcelDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue> //where TValue : struct
    {
        internal string Path { get; set; }
        private protected string sheetname;
        FileInfo fileinfo;

        internal ExcelDictionaryDataProvider(string path, string sheetname)
        {
            Path=path;
            fileinfo = new FileInfo(Path);
            if (!fileinfo.Exists) { throw new System.Exception("Файл не существует"); }
            this.sheetname=sheetname;
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };

            Workbook xlwb = xlapp.Workbooks.Open(fileinfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            XmlSerializableDictionary<TKey, TValue> dct = new XmlSerializableDictionary<TKey, TValue>();
            try
            {
                Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
                rng = rng.Offset[1, 0].Resize[rng.Rows.Count-1, rng.Columns.Count];
                for (int i = 1; i < rng.Rows.Count+1; i++)
                {
                    //TKey key = (TKey)rng.Cells[i, 1].Value;
                    TKey key = Convert.ChangeType(rng.Cells[i, 1].Value, typeof(TKey));

                    dct.Add(key, cellsExtract(rng.Cells[i, 2]));
                }
            }
            finally
            {
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
            return dct;
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };

            Workbook xlwb = xlapp.Workbooks.Open(fileinfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            try
            {
                cellsImport(xlwb, dictionary);
            }
            finally
            {
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
        }


        abstract private protected TValue cellsExtract(Range rng);
        abstract private protected void cellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary);
    }
    internal class ExcelStructDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue> where TValue : struct
    {
        internal ExcelStructDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue cellsExtract(Range rng)
        {
            return ExcelStructIO<TValue>.Read(rng);
        }

        private protected override void cellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            //создаём словарь для индексирования заголовков, заполняем его
            Dictionary<string, int> labelindex = new Dictionary<string, int>();
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            for (int i = 1; i<rng.Columns.Count; i++)
            {
                labelindex.Add(rng[1, i].Value, i);
            }
            //обрезаем заголовки
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count-1, rng.Columns.Count];
            //заполняем строки через ExcelStructIO
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                rng.Cells[counter, 1].Value = keyValue.Key;
                ExcelStructIO<TValue>.Write(keyValue.Value, rng.Range[rng.Cells[counter, 2], rng.Cells[counter, rng.Columns.Count]], labelindex);
                counter++;
            }
        }
    }
    internal static class ExcelStructIO<T> where T : struct
    {
        internal static T Read(Range rng)
        {
            if (typeof(T).IsPrimitive)
                return (T)rng.Value;

            T strct = new T();
            object o = strct;

            FieldInfo[] fieldInfo = typeof(T).GetFields();
            for (int i = 0; i < fieldInfo.Length; i++)
            {
                //не знаю, нужно ли приводить значение ячейки экселя к точному типу перед упаковкой в объект, разобраться позже, пока работает так
                var cellvalue = rng.Offset[0, i].Value;
                if (cellvalue != null)
                    fieldInfo[i].SetValue(o, (object)Convert.ChangeType(cellvalue, fieldInfo[i].FieldType));
            }
            return (T)o;
        }
        internal static void Write(T sourcestruct, Range targetrange, Dictionary<string, int> indexdictionary)
        {
            //принимаем экземпляр структуры для записи, строку для записи (с числом ячеек равным числу полей структуры), словарь индексов заголовков в эксель таблице
            //получаем поля структуры через рефлексию
            List<FieldInfo> list = typeof(T).GetFields().ToList();
            //значение каждого поля записываем в ячейки эксель в соответствии с индексом по имени поля (имя поля должно совпадать с заголовком)
            foreach (FieldInfo fi in list)
            {
                targetrange.Cells[1, indexdictionary[fi.Name]].Value = fi.GetValue(sourcestruct);
            }
        }
    }
    internal class ExcelSimpleDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue>
    {
        internal ExcelSimpleDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue cellsExtract(Range rng)
        {
            return (TValue)rng.Value;
        }

        private protected override void cellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            //обрезаем заголовки
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count-1, rng.Columns.Count];
            //просто записываем пары ключ-значения в 1 и 2 ячейки строки
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                rng.Cells[counter, 1].Value = keyValue.Key;
                rng.Cells[counter, 2].Value = keyValue.Value;
                counter++;
            }
        }
    }


    public static class PropsReloader
    {

        [CommandMethod("RELOADPROPS")]
        public static void ReloadDictionaries()
        {
            Workstation.Editor.WriteMessage("Начало импорта данных. Подождите");
            Reloader(ToReload.Properties | ToReload.Alter | ToReload.Legend | ToReload.LegendDraw);
            Workstation.Editor.WriteMessage("Импорт данных завершён");
        }

        public static void Reloader(ToReload reload)
        {
            if ((reload & ToReload.Properties) == ToReload.Properties)
            {
                ExcelStructDictionaryDataProvider<string, LayerProps> xlpropsprovider = new ExcelStructDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Excel"), "Props");
                XmlDictionaryDataProvider<string, LayerProps> xmlpropsprovider = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props"));
                LayerPropertiesDictionary.Reload(xmlpropsprovider, xlpropsprovider);
            }
            if ((reload & ToReload.Alter) == ToReload.Alter)
            {
                ExcelSimpleDictionaryDataProvider<string, string> xlalterprovider = new ExcelSimpleDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Excel"), "Alter");
                XmlDictionaryDataProvider<string, string> xmlalterprovider = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter"));
                LayerAlteringDictionary.Reload(xmlalterprovider, xlalterprovider);
            }
            if ((reload & ToReload.Legend) == ToReload.Legend)
            {
                ExcelStructDictionaryDataProvider<string, LegendData> xllegendprovider = new ExcelStructDictionaryDataProvider<string, LegendData>(PathOrganizer.GetPath("Excel"), "Legend");
                XmlDictionaryDataProvider<string, LegendData> xmllegendprovider = new XmlDictionaryDataProvider<string, LegendData>(PathOrganizer.GetPath("Legend"));
                LayerLegendDictionary.Reload(xmllegendprovider, xllegendprovider);
            }
            if ((reload & ToReload.LegendDraw) == ToReload.LegendDraw)
            {
                ExcelStructDictionaryDataProvider<string, LegendDrawTemplate> xllegenddrawprovider = new ExcelStructDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("Excel"), "LegendDraw");
                XmlDictionaryDataProvider<string, LegendDrawTemplate> xmllegenddrawprovider = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw"));
                LayerLegendDrawDictionary.Reload(xmllegenddrawprovider, xllegenddrawprovider);
            }
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
                            lp = LayerPropertiesDictionary.GetValue(checkedname, out lpsuccess, false);
                        }
                        catch (NoPropertiesException)
                        {
                            lpsuccess = false;
                        }

                        //bool lpsuccess = LayerProperties._dictionary.TryGetValue(checkedname, out LayerProps lp);
                        workbook.Worksheets[1].Cells[i, 1].Value = checkedname != "" ? checkedname : ltr.Name;
                        if (lpsuccess)
                        {
                            workbook.Worksheets[1].Cells[i, 2].Value = lp.ConstantWidth;
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
                    workbook.SaveAs(PathOrganizer.BasePath+"ExtractedLayers.xlsx");
                    workbook.Close();
                    xlapp.Quit();
                }
            }
        }
    }

    internal abstract class ExternalDictionary<TKey, TValue>
    {
        private protected Dictionary<TKey, TValue> dictionary { get; set; }

        private protected TValue getValue(TKey key, out bool success)
        {
            success = dictionary.TryGetValue(key, out TValue value);
            return value;
            //выдаёт ошибку, когда возвращает value=null. Поправить после перехода на 6.0
        }

        private protected void reload(DictionaryDataProvider<TKey, TValue> primary, DictionaryDataProvider<TKey, TValue> secondary)
        {
            dictionary = secondary.GetDictionary();
            primary.OverwriteSource(dictionary);
        }

        private protected bool checkKey(TKey key)
        { return dictionary.ContainsKey(key); }
    }

    internal class LayerPropertiesDictionary : ExternalDictionary<string, LayerProps>
    {
        private static LayerPropertiesDictionary instance;
        private Dictionary<string, LayerProps> s_defaultLayerProps = new Dictionary<string, LayerProps>();
        static LayerPropertiesDictionary()
        {
            if(instance == null)
                instance = new LayerPropertiesDictionary();
        }

        internal LayerPropertiesDictionary()
        {
            try
            {
                dictionary = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props")).GetDictionary();

                s_defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 107, Green = 107, Blue = 107 });
                s_defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3 });
                s_defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 192, Green = 168, Blue = 110 });
                s_defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LTScale=0.8, LTName="Continuous", LineWeight=-3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                PropsReloader.Reloader(ToReload.Properties);
            }
        }

        private protected LayerProps getValue(string layername, out bool success, bool enabledefaults = true)
        {
            SimpleLayerParser slp;
            success = false;
            try
            {
                slp = new SimpleLayerParser(layername);
            }
            catch (WrongLayerException)
            {
                if (enabledefaults)
                {
                    success = true;
                    return new LayerProps { ConstantWidth=0.4, LTScale=0.8, LTName="Continuous", LineWeight=-3 };
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
            success = dictionary.TryGetValue(slp.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success=true;
                    return s_defaultLayerProps[slp.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        private protected LayerProps getValue(LayerParser layerparser, out bool success, bool enabledefaults = true)
        {
            success = dictionary.TryGetValue(layerparser.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success=true;
                    return s_defaultLayerProps[layerparser.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        public static LayerProps GetValue(string layername, out bool success, bool enabledefaults = true)
        {
            return instance.getValue(layername, out success, enabledefaults);
        }
        public static void Reload (DictionaryDataProvider<string, LayerProps> primary, DictionaryDataProvider<string, LayerProps> secondary)
        {
            instance.reload(primary, secondary);
        }

        public static bool CheckKey(string key)
        {
            return instance.checkKey(key);
        }
    }

    internal class LayerAlteringDictionary : ExternalDictionary<string, string>
    {
        private static LayerAlteringDictionary instance;
        static LayerAlteringDictionary()
        {
            if (instance == null)
                instance = new LayerAlteringDictionary();
        }
        private LayerAlteringDictionary()
        {
            try
            {
                dictionary = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                PropsReloader.Reloader(ToReload.Alter);
            }
        }

        public static string GetValue(string layername, out bool success)
        {
            return instance.getValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, string> primary, DictionaryDataProvider<string, string> secondary)
        {
            instance.reload(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.checkKey(key);
        }
    }

    internal class LayerLegendDictionary : ExternalDictionary<string, Legend.LegendData>
    {
        private static LayerLegendDictionary instance;
        static LayerLegendDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDictionary();
        }
        LayerLegendDictionary()
        {
            try
            {
                dictionary = new XmlDictionaryDataProvider<string, Legend.LegendData>(PathOrganizer.GetPath("Legend")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                PropsReloader.Reloader(ToReload.Legend);
            }
        }
        public static LegendData GetValue(string layername, out bool success)
        {
            return instance.getValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendData> primary, DictionaryDataProvider<string, LegendData> secondary)
        {
            instance.reload(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.checkKey(key);
        }
    }

    internal class LayerLegendDrawDictionary : ExternalDictionary<string, LegendDrawTemplate>
    {
        private static LayerLegendDrawDictionary instance;
        static LayerLegendDrawDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDrawDictionary();
        }
        LayerLegendDrawDictionary()
        {
            try
            {
                dictionary = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                PropsReloader.Reloader(ToReload.LegendDraw);
            }
        }
        public static LegendDrawTemplate GetValue(string layername, out bool success)
        {
            return instance.getValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendDrawTemplate> primary, DictionaryDataProvider<string, LegendDrawTemplate> secondary)
        {
            instance.reload(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.checkKey(key);
        }
    }


    public struct LayerProps
    {
        public double ConstantWidth;
        public double LTScale;
        public byte Red;
        public byte Green;
        public byte Blue;
        public string LTName;
        public int LineWeight;
    }

    [Flags]
    public enum ToReload
    {
        Properties = 1,
        Alter = 2,
        Legend = 4,
        LegendDraw = 8
    }
}