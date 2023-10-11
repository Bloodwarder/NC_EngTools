using LayerProcessing;
using Legend;
using ModelspaceDraw;
using LayerWorks;
using System.Reflection;
using System.Xml.Serialization;
using Teigha.DatabaseServices;
using Teigha.Runtime;


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
        private readonly static Dictionary<string, string> _pathDictionary = new();
        public static string BasePath { get; private set; }
        static PathOrganizer()
        {
            FileInfo fi = new(Assembly.GetExecutingAssembly().Location);
            BasePath = fi.DirectoryName + "\\LayersData\\";

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

    internal abstract class DictionaryDataProvider<TKey, TValue> where TKey : notnull
    {

        public abstract Dictionary<TKey, TValue> GetDictionary();
        public abstract void OverwriteSource(Dictionary<TKey, TValue> dictionary);

    }

    internal class XmlDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue> where TKey : notnull
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
            XmlSerializer xs = new(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new(FilePath, FileMode.Open))
            {
                XmlSerializableDictionary<TKey, TValue>? dct = xs.Deserialize(fs) as XmlSerializableDictionary<TKey, TValue>;
                return dct!;
            }
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            XmlSerializer xs = new(typeof(XmlSerializableDictionary<TKey, TValue>));
            using (FileStream fs = new(FilePath, FileMode.Create))
            {
                xs.Serialize(fs, dictionary as XmlSerializableDictionary<TKey, TValue>);
            }
        }
    }



    internal abstract class ExternalDictionary<TKey, TValue> where TKey : notnull
    {
        private protected Dictionary<TKey, TValue>? InstanceDictionary { get; set; }

        private protected TValue? GetInstanceValue(TKey key, out bool success)
        {
            success = InstanceDictionary!.TryGetValue(key, out TValue? value);
            return value;
            // Выдаёт ошибку, когда возвращает value=null. Поправить после перехода на 6.0
        }

        private protected void ReloadInstance(DictionaryDataProvider<TKey, TValue> primary, DictionaryDataProvider<TKey, TValue> secondary)
        {
            InstanceDictionary = secondary.GetDictionary();
            primary.OverwriteSource(InstanceDictionary);
        }

        private protected bool CheckInstanceKey(TKey key)
        { return InstanceDictionary!.ContainsKey(key); }
    }

    internal class LayerPropertiesDictionary : ExternalDictionary<string, LayerProps>
    {
        private static readonly LayerPropertiesDictionary instance;
        private readonly Dictionary<string, LayerProps> defaultLayerProps = new();
        static LayerPropertiesDictionary()
        {
            instance ??= new LayerPropertiesDictionary();
        }

        internal LayerPropertiesDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, LayerProps>(PathOrganizer.GetPath("Props")).GetDictionary();

                defaultLayerProps.Add("сущ", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LTName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("дем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LTName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
                defaultLayerProps.Add("пр", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LTName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("неутв", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LTName = "Continuous", LineWeight = -3 });
                defaultLayerProps.Add("ндем", new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LTName = "Continuous", LineWeight = -3, Red = 192, Green = 168, Blue = 110 });
                defaultLayerProps.Add("нреорг", new LayerProps { ConstantWidth = 0.6, LTScale = 0.8, LTName = "Continuous", LineWeight = -3, Red = 107, Green = 107, Blue = 107 });
            }
            catch (FileNotFoundException)
            {
                throw new NotImplementedException("Перезагрузка из экселя отключена. Редактирование данных выводится во внешнюю программу");
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
                    return new LayerProps { ConstantWidth = 0.4, LTScale = 0.8, LTName = "Continuous", LineWeight = -3 };
                }
                else
                {
                    throw new NoPropertiesException("Нет стандартов для слоя");
                }
            }
            success = InstanceDictionary!.TryGetValue(slp.TrueName, out LayerProps layerProps);
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
            success = InstanceDictionary!.TryGetValue(layerparser.TrueName, out LayerProps layerProps);
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
            instance ??= new LayerAlteringDictionary();
        }
        private LayerAlteringDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, string>(PathOrganizer.GetPath("Alter")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                throw new NotImplementedException("Перезагрузка из экселя отключена. Редактирование данных выводится во внешнюю программу");
            }
        }

        public static string? GetValue(string layername, out bool success)
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
            instance ??= new LayerLegendDictionary();
        }
        LayerLegendDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, Legend.LegendData>(PathOrganizer.GetPath("Legend")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                throw new NotImplementedException("Перезагрузка из экселя отключена. Редактирование данных выводится во внешнюю программу");
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
            instance ??= new LayerLegendDrawDictionary();
        }
        LayerLegendDrawDictionary()
        {
            try
            {
                InstanceDictionary = new XmlDictionaryDataProvider<string, LegendDrawTemplate>(PathOrganizer.GetPath("LegendDraw")).GetDictionary();
            }
            catch (FileNotFoundException)
            {
                throw new NotImplementedException("Перезагрузка из экселя отключена. Редактирование данных выводится во внешнюю программу");
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
        public string LTName;
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