using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Teigha.Runtime;
using LayerWorks;
using Teigha.DatabaseServices;
using LayerProcessing;
using System;
using Legend;
using ModelspaceDraw;
using Loader.CoreUtilities;

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

    internal abstract class DictionaryDataProvider<TKey, TValue>
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }

    internal class XmlDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue>
    {
        private string FilePath { get; set; }
        private readonly FileInfo _fileInfo;

        internal XmlDictionaryDataProvider(string path)
        {
            FilePath = path;
            _fileInfo = new FileInfo(FilePath);
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(FilePath, FileMode.Open))
            {
                XmlSerializableDictionary<TKey, TValue> dct = xs.Deserialize(fs) as XmlSerializableDictionary<TKey, TValue>;
                return dct;
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new FileStream(FilePath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }
    }

    abstract internal class ExcelDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue> //where TValue : struct
    {
        internal string Path { get; set; }
        private protected string sheetname;
        private readonly FileInfo _fileInfo;

        internal ExcelDictionaryDataProvider(string path, string sheetname)
        {
            Path = path;
            _fileInfo = new FileInfo(Path);
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            this.sheetname = sheetname;
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };

            Workbook xlwb = xlapp.Workbooks.Open(_fileInfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            XmlSerializableDictionary<TKey, TValue> dct = new XmlSerializableDictionary<TKey, TValue>();
            try
            {
                Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
                rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
                for (int i = 1; i < rng.Rows.Count + 1; i++)
                {
                    //TKey key = (TKey)rng.Cells[i, 1].Value;
                    TKey key = Convert.ChangeType(rng.Cells[i, 1].Value, typeof(TKey));

                    dct.Add(key, CellsExtract(rng.Cells[i, 2]));
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

            Workbook xlwb = xlapp.Workbooks.Open(_fileInfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            try
            {
                CellsImport(xlwb, dictionary);
            }
            finally
            {
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
        }


        abstract private protected TValue CellsExtract(Range rng);
        abstract private protected void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary);
    }
    internal class ExcelStructDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue> where TValue : struct
    {
        internal ExcelStructDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(Range rng)
        {
            return ExcelStructIO<TValue>.Read(rng);
        }

        private protected override void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Создаём словарь для индексирования заголовков, заполняем его
            Dictionary<string, int> labelindex = new Dictionary<string, int>();
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            for (int i = 1; i < rng.Columns.Count; i++)
            {
                labelindex.Add(rng[1, i].Value, i);
            }
            // Обрезаем заголовки
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
            // Заполняем строки через ExcelStructIO
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
                // Не знаю, нужно ли приводить значение ячейки экселя к точному типу перед упаковкой в объект, разобраться позже, пока работает так
                var cellvalue = rng.Offset[0, i].Value;
                if (cellvalue != null)
                    fieldInfo[i].SetValue(o, (object)Convert.ChangeType(cellvalue, fieldInfo[i].FieldType));
            }
            return (T)o;
        }
        internal static void Write(T sourcestruct, Range targetrange, Dictionary<string, int> indexdictionary)
        {
            // Принимаем экземпляр структуры для записи, строку для записи (с числом ячеек равным числу полей структуры), словарь индексов заголовков в эксель таблице
            // Получаем поля структуры через рефлексию
            List<FieldInfo> list = typeof(T).GetFields().ToList();
            // Значение каждого поля записываем в ячейки эксель в соответствии с индексом по имени поля (имя поля должно совпадать с заголовком)
            foreach (FieldInfo fi in list)
            {
                targetrange.Cells[1, indexdictionary[fi.Name]].Value = fi.GetValue(sourcestruct);
            }
        }
    }
    internal class ExcelSimpleDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue>
    {
        internal ExcelSimpleDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(Range rng)
        {
            return (TValue)rng.Value;
        }

        private protected override void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Обрезаем заголовки
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
            // Просто записываем пары ключ-значения в 1 и 2 ячейки строки
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                rng.Cells[counter, 1].Value = keyValue.Key;
                rng.Cells[counter, 2].Value = keyValue.Value;
                counter++;
            }
        }
    }

    /// <summary>
    /// Загрузчик данных
    /// </summary>
    public static class ExternalDataLoader
    {
        /// <summary>
        /// Команда для перезагрузки словарей с данными
        /// </summary>
        [CommandMethod("RELOADPROPS")]
        public static void ReloadDictionaries()
        {
            Workstation.Define();
            Workstation.Editor.WriteMessage("Начало импорта данных. Подождите");
            Reloader(ToReload.Properties | ToReload.Alter | ToReload.Legend | ToReload.LegendDraw);
            Workstation.Editor.WriteMessage("Импорт данных завершён");
        }

        /// <summary>
        /// Перезагрузка словарей с данными
        /// </summary>
        /// <param name="reload">Словари к перезагрузке</param>
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

        /// <summary>
        /// Выгрузка слоёв чертежа в Excel
        /// </summary>
        [CommandMethod("EXTRACTLAYERS")]
        public static void ExtractLayersInfoToExcel()
        {
            Application xlapp = new Application
            { DisplayAlerts = false };
            Workbook workbook = xlapp.Workbooks.Add();
            HostMgd.ApplicationServices.Document doc = HostMgd.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            TransactionManager tm = HostApplicationServices.WorkingDatabase.TransactionManager;
            Transaction transaction = tm.StartTransaction();
            using (transaction)
            {
                LayerTable lt = tm.TopTransaction.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                var layers = (from ObjectId elem in lt
                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForRead)
                              select ltr).ToList();
                int i = 1;
                try
                {
                    foreach (LayerTableRecord ltr in layers)
                    {
                        string checkedname = "";
                        LayerProps lp = new LayerProps();
                        // Попытка распарсить имя слоя для поиска существующих сохранённых свойств
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
                        LinetypeTableRecord lttr = transaction.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead) as LinetypeTableRecord;
                        workbook.Worksheets[1].Cells[i, 7].Value = lttr.Name;
                        workbook.Worksheets[1].Cells[i, 8].Value = ltr.LineWeight;
                        i++;
                    }
                }
                finally
                {
                    workbook.SaveAs(PathOrganizer.BasePath + "ExtractedLayers.xlsx");
                    workbook.Close();
                    xlapp.Quit();
                }
            }
        }
    }

    internal abstract class ExternalDictionary<TKey, TValue>
    {
        private protected Dictionary<TKey, TValue> InstanceDictionary { get; set; }

        private protected TValue GetInstanceValue(TKey key, out bool success)
        {
            success = InstanceDictionary.TryGetValue(key, out TValue value);
            return value;
            // Выдаёт ошибку, когда возвращает value=null. Поправить после перехода на 6.0
        }

        private protected void ReloadInstance(DictionaryDataProvider<TKey, TValue> primary, DictionaryDataProvider<TKey, TValue> secondary)
        {
            InstanceDictionary = secondary.GetDictionary();
            primary.OverwriteSource(InstanceDictionary);
        }

        private protected bool CheckInstanceKey(TKey key)
        { return InstanceDictionary.ContainsKey(key); }
    }

    internal class LayerPropertiesDictionary : ExternalDictionary<string, LayerProps>
    {
        private static readonly LayerPropertiesDictionary instance;
        private readonly Dictionary<string, LayerProps> defaultLayerProps = new Dictionary<string, LayerProps>();
        static LayerPropertiesDictionary()
        {
            if (instance == null)
                instance = new LayerPropertiesDictionary();
        }

        internal LayerPropertiesDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props")).GetDictionary();

                defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
                defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 192, Green = 168, Blue = 110 });
                defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Properties);
            }
        }

        private protected LayerProps GetInstanceValue(string layername, out bool success, bool enabledefaults = true)
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
                    return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LineTypeName = "Continuous", LineWeight = -3 };
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
            success = InstanceDictionary.TryGetValue(slp.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success = true;
                    return defaultLayerProps[slp.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        private protected LayerProps GetInstanceValue(LayerParser layerparser, out bool success, bool enabledefaults = true)
        {
            success = InstanceDictionary.TryGetValue(layerparser.TrueName, out LayerProps layerProps);
            if (success)
            {
                return layerProps;
            }
            else
            {
                if (enabledefaults)
                {
                    success = true;
                    return defaultLayerProps[layerparser.BuildStatusText];
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
        }

        public static LayerProps GetValue(string layername, out bool success, bool enabledefaults = true)
        {
            return instance.GetInstanceValue(layername, out success, enabledefaults);
        }
        public static LayerProps GetValue(LayerParser layer, out bool success, bool enabledefaults = true)
        {
            return instance.GetInstanceValue(layer, out success, enabledefaults);
        }
        public static void Reload(DictionaryDataProvider<string, LayerProps> primary, DictionaryDataProvider<string, LayerProps> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }

        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }

    internal class LayerAlteringDictionary : ExternalDictionary<string, string>
    {
        private static readonly LayerAlteringDictionary instance;
        static LayerAlteringDictionary()
        {
            if (instance == null)
                instance = new LayerAlteringDictionary();
        }
        private LayerAlteringDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Alter);
            }
        }

        public static string GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, string> primary, DictionaryDataProvider<string, string> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }

    internal class LayerLegendDictionary : ExternalDictionary<string, Legend.LegendData>
    {
        private static readonly LayerLegendDictionary instance;
        static LayerLegendDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDictionary();
        }
        LayerLegendDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, Legend.LegendData>(PathOrganizer.GetPath("Legend")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.Legend);
            }
        }
        public static LegendData GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendData> primary, DictionaryDataProvider<string, LegendData> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }

    internal class LayerLegendDrawDictionary : ExternalDictionary<string, LegendDrawTemplate>
    {
        private static readonly LayerLegendDrawDictionary instance;
        static LayerLegendDrawDictionary()
        {
            if (instance == null)
                instance = new LayerLegendDrawDictionary();
        }
        LayerLegendDrawDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                ExternalDataLoader.Reloader(ToReload.LegendDraw);
            }
        }
        public static LegendDrawTemplate GetValue(string layername, out bool success)
        {
            return instance.GetInstanceValue(layername, out success);
        }
        public static void Reload(DictionaryDataProvider<string, LegendDrawTemplate> primary, DictionaryDataProvider<string, LegendDrawTemplate> secondary)
        {
            instance.ReloadInstance(primary, secondary);
        }
        public static bool CheckKey(string key)
        {
            return instance.CheckInstanceKey(key);
        }
    }

    /// <summary>
    /// Свойства слоя
    /// </summary>
    public struct LayerProps
    {
        /// <summary>
        /// Глобальная ширина
        /// </summary>
        public double ConstantWidth;
        /// <summary>
        /// Масштаб типа линий
        /// </summary>
        public double LTScale;
        /// <summary>
        /// Красный
        /// </summary>
        public byte Red;
        /// <summary>
        /// Зелёный
        /// </summary>
        public byte Green;
        /// <summary>
        /// Синий
        /// </summary>
        public byte Blue;
        /// <summary>
        /// Тип линий
        /// </summary>
        public string LineTypeName;
        /// <summary>
        /// Вес линий
        /// </summary>
        public int LineWeight;
    }

    /// <summary>
    /// Словари к перезагрузке
    /// </summary>
    [Flags]
    public enum ToReload
    {
        /// <summary>
        /// Свойства
        /// </summary>
        Properties = 1,
        /// <summary>
        /// Альтернативные типы
        /// </summary>
        Alter = 2,
        /// <summary>
        /// Информация для компоновки условных обозначений
        /// </summary>
        Legend = 4,
        /// <summary>
        /// Информация для отрисовки условных обозначений
        /// </summary>
        LegendDraw = 8
    }
}