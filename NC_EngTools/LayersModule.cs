using ExternalData;
using HostMgd.ApplicationServices;
using Loader.CoreUtilities;
using LayerWorks;
using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;


namespace LayerProcessing
{
    /// <summary>
    /// Парсер слоя, получающий информацию об объекте на основе имени
    /// </summary>
    public abstract class LayerParser
    {
        /// <summary>
        /// Имя исходного слоя, переданного в конструктор 
        /// </summary>
        public string InputLayerName { get; private set; }
        /// <summary>
        /// Стандартный префикс имени слоя для фильтрации слоёв, которые парсер будет пытаться обработать
        /// </summary>
        public static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
        /// <summary>
        /// Имя внешнего проекта, согласно которому нанесены объекты слоя
        /// </summary>
        public string ExtProjectName { get; private set; }
        /// <summary>
        /// Тип объекта инженерной инфраструктуры
        /// </summary>
        public string EngType { get; private set; }
        /// <summary>
        /// Тип геометрии
        /// </summary>
        public string GeomType { get; private set; }
        /// <summary>
        /// Основное имя, отражающее конкретный тип объекта без привязки к статусу и дополнительным данным
        /// </summary>
        public string MainName { get; private set; }
        /// <summary>
        /// Статус объекта
        /// </summary>
        public Status BuildStatus { get; private set; }
        /// <summary>
        /// Текст статуса объекта
        /// </summary>
        public string BuildStatusText { get { return st_txt[(int)BuildStatus]; } }
        /// <summary>
        /// Имя выходного слоя с учётом произведённых изменений данных
        /// </summary>
        public string OutputLayerName
        {
            get
            {
                List<string> recomp = new List<string>
                {
                    StandartPrefix
                };
                if (extpr) { recomp.Add("[" + ExtProjectName + "]"); }
                recomp.Add(MainName);
                recomp.Add(BuildStatusText);
                if (recstatus) { recomp.Add("пер"); }
                return string.Join("_", recomp.ToArray());
            }
        }

        /// <summary>
        /// "Истинное" имя объекта с учётом статуса, но с отброшенными дополнительными данными
        /// </summary>
        public string TrueName
        //string for props compare (type of network and it's status)
        {
            get
            {
                return string.Join("_", new string[2] { MainName, st_txt[(int)BuildStatus] });
            }
        }
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "ндем", "нреорг", "неутв" };

        /// <summary>
        /// Относится ли объект к переустройству существующих
        /// </summary>
        protected bool recstatus = false;
        /// <summary>
        /// Относится ли объект к конкретному именованному внешнему проекту
        /// </summary>
        protected bool extpr = false;
        /// <summary>
        /// Назначен ли для объекта тип геометрии
        /// </summary>
        protected bool geomassigned = false;

        /// <summary>
        /// Конструктор класса LayerParser
        /// </summary>
        /// <param name="layername">Строка с именем исходного слоя</param>
        /// <exception cref="WrongLayerException">Ошибка обработки имени слоя</exception>
        public LayerParser(string layername)
        {
            InputLayerName = layername;
            if (!layername.StartsWith(StandartPrefix))
            {
                throw new WrongLayerException($"Слой {layername} не обрабатывается программой");
                //обработать при передаче слоёв
            }
            string[] decomp = InputLayerName.Split('_');
            int mainnamestart = 1;
            int mainnameend;
            int counter = 1;
            if (decomp[1].StartsWith("["))
            {
                List<string> list = new List<string>
                {
                    decomp[1]
                };
                for (int i = counter + 1; i < decomp.Length; i++)
                {
                    if (!decomp[i - 1].EndsWith("]"))
                    { list.Add(decomp[i]); }
                    else
                    { break; }
                }
                ExtProjectName = string.Join("_", list.ToArray()).Replace("[", "").Replace("]", "");
                extpr = true;
                counter = +list.Count + 1;
                mainnamestart = counter;
            }
            if (decomp[counter].Length == 2)
            {
                EngType = decomp[counter];
                counter++;
            }
            if (decomp[counter].Length == 1)
            {
                GeomType = decomp[counter];
                geomassigned = true;
            }
            if (decomp[decomp.Length - 1] == "пер")
            {
                recstatus = true;
                mainnameend = decomp.Length - 3;
            }
            else
            {
                mainnameend = decomp.Length - 2;
            }
            string str = recstatus ? decomp[decomp.Length - 2] : decomp[decomp.Length - 1];
            bool stfound = false;
            for (int i = 0; i < st_txt.Length; i++) //searching and assigning status index (IMPROVE CODE LATER)
            {
                if (st_txt[i] == str)
                {
                    BuildStatus = (Status)i; stfound = true; break;
                }
            }
            if (!stfound) { throw new WrongLayerException($"В слое {layername} не найден статус"); }

            MainName = string.Join("_", decomp.Skip(mainnamestart).Take(mainnameend - mainnamestart + 1));
        }
        /// <summary>
        /// Изменить статус объекта
        /// </summary>
        /// <param name="newstatus">Новый статус объекта</param>
        public void StatusSwitch(Status newstatus)
        {
            BuildStatus = newstatus;
            //disabling reconstruction marker for existing objects layers
            if (recstatus & !(BuildStatus == Status.Planned || BuildStatus == Status.NSPlanned || BuildStatus == Status.NSReorg))
            {
                recstatus = false;
            }
            //disabling external project tag for current project and existing layers
            if (extpr & !(BuildStatus == Status.NSPlanned || BuildStatus == Status.NSDeconstructing || BuildStatus == Status.NSReorg))
            {
                ExtProjectName = "";
                extpr = false;
            }
            return;
        }

        /// <summary>
        /// Пометить объект как переустраиваемый или снять отметку
        /// </summary>
        public void ReconstrSwitch()
        {
            if (BuildStatus == Status.Planned || BuildStatus == Status.NSPlanned || BuildStatus == Status.NSReorg) //filter only planned layers
            {
                if (recstatus)
                {
                    recstatus = false;
                }
                else
                {
                    recstatus = true;
                }
            }
            else
            {
                BuildStatus = Status.Planned;
                recstatus = true;
            }
            return;
        }
        /// <summary>
        /// Назначить имя внешнего проекта, в соответствии с которым отображён объект слоя
        /// </summary>
        /// <param name="newprojname">Имя внешнего проекта</param>
        public void ExtProjNameAssign(string newprojname)
        {
            bool emptyname = newprojname == "";
            if (!emptyname)
            {
                switch (BuildStatus)
                {
                    case Status.Planned:
                    case Status.Existing:
                        BuildStatus = Status.NSPlanned;
                        break;
                    case Status.Deconstructing:
                        BuildStatus = Status.NSDeconstructing;
                        break;
                }
            }

            if (extpr)
            {
                if (!emptyname) //replacing name
                {
                    ExtProjectName = newprojname;
                }
                else //erasing name
                {
                    ExtProjectName = "";
                    extpr = false;
                }
            }
            else if (!emptyname) //assigning name
            {
                ExtProjectName = newprojname;
                extpr = true;
            }
        }
        /// <summary>
        /// Изменить тип объекта на альтернативный (например - самотечную сеть канализации на напорную, кабельную ЛЭП на воздушную и т.п.)
        /// </summary>
        public void Alter()
        {
            string str = LayerAlteringDictionary.GetValue(MainName, out bool success);
            if (!success) { return; }
            MainName = str;
        }

        /// <summary>
        /// Назначить выходное имя слоя связанному объекту
        /// </summary>
        public abstract void Push();
    }

    /// <summary>
    /// Статус объекта
    /// </summary>
    public enum Status : int
    {
        /// <summary>
        /// Существующий
        /// </summary>
        Existing = 0,
        /// <summary>
        /// Демонтируемый
        /// </summary>
        Deconstructing = 1,
        /// <summary>
        /// Планируемый
        /// </summary>
        Planned = 2,
        /// <summary>
        /// Демонтируемый-неутверждаемый
        /// </summary>
        NSDeconstructing = 3,
        /// <summary>
        /// Неутверждаемый-планируемый реорганизуемый
        /// </summary>
        NSReorg = 4,
        /// <summary>
        /// Неутверждаемый-планируемый
        /// </summary>
        NSPlanned = 5
    }

    /// <summary>
    /// Парсер, обрабатывающий только строки с именами без связанного объекта
    /// </summary>
    public class SimpleLayerParser : LayerParser
    {
        /// <inheritdoc/>
        public SimpleLayerParser(string layername) : base(layername) { }
        /// <summary>
        /// Не работает, так как парсер обрабатывает только строку без связанного объекта
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Парсер, связанный с текущим слоем
    /// </summary>
    public class CurrentLayerParser : LayerParser
    {
        /// <summary>
        /// Конструктор без параметров, автоматически передающий в базовый конструктор имя текущего слоя
        /// </summary>
        public CurrentLayerParser() : base(Clayername()) { ActiveLayerParsers.Add(this); }

        private static string Clayername()
        {
            Database db = Workstation.Database;
            LayerTableRecord ltr = db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
            return ltr.Name;
        }

        /// <summary>
        /// Задаёт стандартные свойства для черчения новых объектов чертежа
        /// </summary>
        public override void Push()
        {
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;

            LayerChecker.Check(this);
            LayerTable lt = tm.TopTransaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            db.Clayer = lt[OutputLayerName];
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out _);
            db.Celtscale = lp.LTScale;
            db.Plinewid = lp.ConstantWidth;
        }
    }

    /// <summary>
    /// Парсер, связанный с объектом чертежа (Entity)
    /// </summary>
    public class EntityLayerParser : LayerParser
    {
        internal EntityLayerParser(string layername) : base(layername)
        {
            ActiveLayerParsers.Add(this);
        }
        /// <summary>
        /// Конструктор, принимающий объект чертежа
        /// </summary>
        /// <param name="entity"></param>
        public EntityLayerParser(Entity entity) : base(entity.Layer)
        {
            BoundEntities.Add(entity); ActiveLayerParsers.Add(this);
        }
        /// <summary>
        /// Коллекция связанных объектов чертежа
        /// </summary>
        public List<Entity> BoundEntities = new List<Entity>();
        /// <summary>
        /// Назначение выходного слоя и соответствующих ему свойств связанным объектам чертежа
        /// </summary>
        public override void Push()
        {
            LayerChecker.Check(this);
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out _);
            foreach (Entity ent in BoundEntities)
            {
                ent.Layer = OutputLayerName;
                if (ent is Polyline pl)
                {
                    pl.LinetypeScale = lp.LTScale;
                    pl.ConstantWidth = lp.ConstantWidth;
                }
            }
        }
    }
    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord
    /// </summary>
    public class RecordLayerParser : LayerParser
    {
        private DBObjectWrapper<LayerTableRecord> _boundLayer;


        /// <summary>
        /// Связанный слой (объект LayerTableRecord)
        /// </summary>
        public LayerTableRecord BoundLayer 
        { 
            get => _boundLayer.Get();
            set => _boundLayer = new DBObjectWrapper<LayerTableRecord>(value, OpenMode.ForWrite); 
        }

        /// <summary>
        /// Конструктор, принимающий объект LayerTableRecord
        /// </summary>
        /// <param name="ltr">Запись таблицы слоёв</param>
        public RecordLayerParser(LayerTableRecord ltr) : base(ltr.Name)
        {
            BoundLayer = ltr;
        }
        /// <summary>
        /// Изменяет имя и свойства связанного слоя слоя
        /// </summary>
        /// <exception cref="NotImplementedException">Метод не реализован (пока не понадобился)</exception>
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Парсер, связанный с объектом LayerTableRecord, хранящий данные об исходном цвете и видимости слоя
    /// </summary>
    public class ChapterStoreLayerParser : RecordLayerParser
    {
        const byte redproj = 0; const byte greenproj = 255; const byte blueproj = 255;
        const byte redns = 0; const byte greenns = 153; const byte bluens = 153;
        /// <summary>
        /// Исходное состояние видимости слоя
        /// </summary>
        public bool StoredEnabledState;
        /// <summary>
        /// Исходный цвет слоя
        /// </summary>
        public Teigha.Colors.Color StoredColor;
        /// <inheritdoc/>
        public ChapterStoreLayerParser(LayerTableRecord ltr) : base(ltr)
        {
            StoredEnabledState = ltr.IsOff;
            StoredColor = ltr.Color;
            ChapterStoredLayerParsers.Add(this);
        }
        /// <summary>
        /// Возврат исходного цвета и видимости слоя
        /// </summary>
        public void Reset()
        {
            //BoundLayer = Workstation.TransactionManager.TopTransaction.GetObject(BoundLayer.Id, OpenMode.ForWrite) as LayerTableRecord;
            BoundLayer.IsOff = StoredEnabledState;
            BoundLayer.Color = StoredColor;
        }
        /// <summary>
        /// Возврат исходного цвета и видимости слоя
        /// </summary>
        public override void Push()
        {
            Reset();
        }

        /// <summary>
        /// Принимает тип объектов. Если объект не относится к заданному типу - выключает его. Если относится к переустройству - задаёт яркий цвет
        /// </summary>
        /// <param name="engtype">Тип объекта</param>
        public void Push(string engtype)
        {
            if (engtype == null)
            {
                Reset();
                return;
            }

            if (EngType == engtype)
            {
                BoundLayer.IsOff = false;
                if (recstatus)
                {
                    if (BuildStatus == Status.Planned)
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redproj, greenproj, blueproj);
                    }
                    else if (BuildStatus == Status.NSPlanned)
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redns, greenns, bluens);
                    }
                }
            }
            else
            {
                BoundLayer.IsOff = true;
            }
        }
    }

    internal static class ActiveLayerParsers
    {
        private static List<LayerParser> List { get; set; } = new List<LayerParser>();
        internal static void StatusSwitch(Status status)
        {
            foreach (LayerParser lp in List) { lp.StatusSwitch(status); };
        }
        internal static void Alter()
        {
            foreach (LayerParser lp in List) { lp.Alter(); }
        }
        internal static void ReconstrSwitch()
        {
            foreach (LayerParser lp in List) { lp.ReconstrSwitch(); }
        }
        internal static void ExtProjNameAssign(string extprojname)
        {
            foreach (LayerParser lp in List) { lp.ExtProjNameAssign(extprojname); }
        }
        internal static void Add(LayerParser lp)
        {
            List.Add(lp);
        }
        internal static void Push()
        {
            foreach (LayerParser lp in List) { lp.Push(); }
        }
        internal static void Flush()
        {
            List.Clear();
        }
    }

    internal static class ChapterStoredLayerParsers
    {
        private static readonly Dictionary<Document, bool> _eventAssigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<ChapterStoreLayerParser>> StoredLayerStates { get; } = new Dictionary<Document, List<ChapterStoreLayerParser>>();
        internal static void Add(ChapterStoreLayerParser lp)
        {
            Document doc = Workstation.Document;
            // Сохранить парсер в словарь по ключу-текущему документу
            if (!StoredLayerStates.ContainsKey(doc))
            {
                StoredLayerStates[doc] = new List<ChapterStoreLayerParser>();
                _eventAssigned.Add(doc, false);
            }
            if (!StoredLayerStates[doc].Any(l => l.InputLayerName == lp.InputLayerName))
            {
                StoredLayerStates[doc].Add(lp);
            }
        }
        // восстановление состояний слоёв при вызове командой
        internal static void Reset()
        {
            Document doc = Workstation.Document;
            foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            _eventAssigned[doc] = false;
        }

        // восстановление состояний слоёв при вызове событием сохранения чертежа
        internal static void Reset(object sender, EventArgs e)
        {
            Database db = sender as Database;
            Document doc = Application.DocumentManager.GetDocument(db);
            Teigha.DatabaseServices.TransactionManager tm = db.TransactionManager; //Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc])
                {
                    LayerTableRecord _ = (LayerTableRecord)transaction.GetObject(lp.BoundLayer.Id, OpenMode.ForWrite);
                    lp.Reset();
                }

                doc.Database.BeginSave -= Reset;
                _eventAssigned[doc] = false;
                ChapterVisualizer.ActiveChapterState[doc] = null;
                Flush(doc);
                transaction.Commit();
            }

        }
        internal static void Highlight(string engtype)
        {
            Document doc = Workstation.Document;
            if (!_eventAssigned[doc])
            {
                doc.Database.BeginSave += Reset;
                _eventAssigned[doc] = true;
            }
            foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc]) { lp.Push(engtype); }
        }

        //Сбросить сохранённые состояния слоёв для текущего документа
        internal static void Flush(Document doc = null)
        {
            if (doc == null)
                doc = Workstation.Document;
            foreach (ChapterStoreLayerParser cslp in StoredLayerStates[doc])
                cslp.BoundLayer.Dispose();
            StoredLayerStates[doc].Clear();
        }
    }
    internal class WrongLayerException : System.Exception
    {
        public WrongLayerException(string message) : base(message) { }
    }
    internal class NoPropertiesException : System.Exception
    {
        public NoPropertiesException(string message) : base(message) { }
    }
}


