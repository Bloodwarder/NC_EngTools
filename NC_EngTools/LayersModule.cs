using System.Linq;
using System;
using System.Collections.Generic;
using Teigha.DatabaseServices;
using HostMgd.ApplicationServices;
using ExternalData;
using NC_EngTools;


namespace LayerProcessing
{
    public abstract class LayerParser
    {
        public string InputLayerName { get; private set; }
        public static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
        public string ExtProjectName { get; private set; }
        public string EngType { get; private set; }
        public string GeomType { get; private set; }
        public string MainName { get; private set; }
        public Status BuildStatus { get; private set; }
        public string BuildStatusText { get { return st_txt[(int)BuildStatus]; } }
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


        public string TrueName
        //string for props compare (type of network and it's status)
        {
            get
            {
                return string.Join("_", new string[2] { MainName, st_txt[(int)BuildStatus] });
            }
        }
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "ндем", "нреорг", "неутв" };

        protected bool recstatus = false;
        protected bool extpr = false;
        protected bool geomassigned = false;

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

        public void Alter()
        {
            string str = LayerAlteringDictionary.GetValue(MainName, out bool success);
            if (!success) { return; }
            MainName = str;
        }

        public abstract void Push();
    }
    public enum Status : int
    {
        Existing = 0,
        Deconstructing = 1,
        Planned = 2,
        NSDeconstructing = 3,
        NSReorg = 4,
        NSPlanned = 5
    }

    public class SimpleLayerParser : LayerParser
    {
        public SimpleLayerParser(string layername) : base(layername) { }
        public override void Push()
        {
            throw new NotImplementedException();
        }
    }

    internal class CurrentLayerParser : LayerParser
    {
        internal CurrentLayerParser() : base(Clayername()) { ActiveLayerParsers.Add(this); }

        private static string Clayername()
        {
            Database db = Workstation.Database;
            LayerTableRecord ltr = db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord;
            return ltr.Name;
        }

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
    internal class EntityLayerParser : LayerParser
    {
        internal EntityLayerParser(string layername) : base(layername)
        {
            ActiveLayerParsers.Add(this);
        }
        internal EntityLayerParser(Entity ent) : base(ent.Layer)
        {
            ObjectList.Add(ent); ActiveLayerParsers.Add(this);
        }

        internal List<Entity> ObjectList = new List<Entity>();
        public override void Push()
        {
            LayerChecker.Check(this);
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out _);
            foreach (Entity ent in ObjectList)
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

    internal class RecordLayerParser : LayerParser
    {
        internal LayerTableRecord BoundLayer;

        internal RecordLayerParser(LayerTableRecord ltr) : base(ltr.Name)
        {
            BoundLayer = ltr;
        }

        public override void Push()
        {
            throw new NotImplementedException();
        }
    }

    internal class ChapterStoreLayerParser : RecordLayerParser
    {
        const byte redproj = 0; const byte greenproj = 255; const byte blueproj = 255;
        const byte redns = 0; const byte greenns = 153; const byte bluens = 153;
        internal bool StoredEnabledState;
        internal Teigha.Colors.Color StoredColor;
        internal ChapterStoreLayerParser(LayerTableRecord ltr) : base(ltr)
        {
            StoredEnabledState = ltr.IsOff;
            StoredColor = ltr.Color;
            ChapterStoredRecordLayerParsers.Add(this);
        }
        public void Reset()
        {
            //BoundLayer = Workstation.TransactionManager.TopTransaction.GetObject(BoundLayer.Id, OpenMode.ForWrite) as LayerTableRecord;
            BoundLayer.IsOff = StoredEnabledState;
            BoundLayer.Color = StoredColor;
        }
        public override void Push()
        {
            Reset();
        }

        public void Push(string engtype)
        {
            if (engtype == null)
            {
                Reset();
                return;
            }

            if (base.EngType == engtype)
            {
                BoundLayer.IsOff = false;
                if (base.recstatus)
                {
                    if (base.BuildStatus == Status.Planned)
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redproj, greenproj, blueproj);
                    }
                    else if (base.BuildStatus == Status.NSPlanned)
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

    internal static class ChapterStoredRecordLayerParsers
    {
        private static readonly Dictionary<Document, bool> _eventAssigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<ChapterStoreLayerParser>> StoredLayerStates { get; } = new Dictionary<Document, List<ChapterStoreLayerParser>>();
        internal static void Add(ChapterStoreLayerParser lp)
        {
            Document doc = Workstation.Document;
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
        internal static void Reset()
        {
            Document doc = Workstation.Document;
            foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            _eventAssigned[doc] = false;
        }

        internal static void Reset(object sender, EventArgs e)
        {
            Document doc = Workstation.Document;
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;

            using (Transaction transaction = tm.StartTransaction())
            {
                foreach (ChapterStoreLayerParser lp in StoredLayerStates[doc])
                {
                    LayerTableRecord ltr = (LayerTableRecord)transaction.GetObject(lp.BoundLayer.Id, OpenMode.ForWrite);
                    lp.Reset();
                }

                doc.Database.BeginSave -= Reset;
                _eventAssigned[doc] = false;
                ChapterVisualizer.ActiveChapterState[doc] = null;
                Flush();
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
        internal static void Flush()
        {
            Document doc = Workstation.Document;
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


