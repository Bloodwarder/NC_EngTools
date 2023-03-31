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
        public string BuildStatus { get { return st_txt[bldstatus]; } }
        public string OutputLayerName
        {
            get
            {
                List<string> recomp = new List<string>();
                recomp.Add(StandartPrefix);
                if (extpr) { recomp.Add("["+ExtProjectName+"]"); }
                recomp.Add(MainName);
                recomp.Add(BuildStatus);
                if (recstatus) { recomp.Add("пер"); }
                return string.Join("_", recomp.ToArray());
            }
        }


        public string TrueName
        //string for props compare (type of network and it's status)
        {
            get
            {
                return string.Join("_", new string[2] { MainName, st_txt[bldstatus] });
            }
        }
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "неутв", "ндем", "нреорг" };

        protected bool recstatus = false;
        protected bool extpr = false;
        protected bool geomassigned = false;
        protected int bldstatus;

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
                List<string> list = new List<string>();
                list.Add(decomp[1]);
                for (int i = counter+1; i < decomp[1].Length; i++)
                {
                    if (!decomp[i-1].EndsWith("]"))
                    { list.Add(decomp[i]); }
                    else
                    { break; }
                }
                ExtProjectName =string.Join("_", list.ToArray()).Replace("[", "").Replace("]", "");
                extpr = true;
                counter =+list.Count + 1;
                mainnamestart=counter;
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
                counter++;
            }
            if (decomp[decomp.Length-1]=="пер")
            {
                recstatus = true;
                mainnameend = decomp.Length-3;
            }
            else
            {
                mainnameend = decomp.Length-2;
            }
            string str = recstatus ? decomp[decomp.Length-2] : decomp[decomp.Length-1];
            bool stfound = false;
            for (int i = 0; i<st_txt.Length; i++) //searching and assigning status index (IMPROVE CODE LATER)
            {
                if (st_txt[i]==str)
                {
                    bldstatus=i; stfound = true; break;
                }
            }
            if (!stfound) { throw new WrongLayerException($"В слое {layername} не найден статус"); }

            MainName = string.Join("_", decomp.Skip(mainnamestart).Take(mainnameend-mainnamestart+1));
        }

        public void StatusSwitch(Status newstatus)
        {
            bldstatus=(int)newstatus;
            //disabling reconstruction marker for existing objects layers
            if (recstatus & !(bldstatus==2||bldstatus==3||bldstatus==5))
            {
                recstatus=false;
            }
            //disabling external project tag for current project and existing layers
            if (extpr & !(bldstatus==3||bldstatus==4||bldstatus==5))
            {
                ExtProjectName = "";
                extpr = false;
            }
            return;
        }

        public void ReconstrSwitch()
        {
            if (bldstatus==2||bldstatus==3||bldstatus==5) //filter only planned layers
            {
                if (recstatus)
                {
                    recstatus=false;
                }
                else
                {
                    recstatus=true;
                }
            }
            else
            {
                bldstatus = 2;
                recstatus = true;
            }
            return;
        }

        public void ExtProjNameAssign(string newprojname)
        {
            bool emptyname = newprojname == "";
            if (!emptyname&!(bldstatus==3||bldstatus==4||bldstatus==5))
            {
                bldstatus = 3;
            } //assigning NSPlanned status when current project layer processed (non NS)
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

        public enum Status : int
        {
            Existing = 0,
            Deconstructing = 1,
            Planned = 2,
            NSPlanned = 3,
            NSDeconstructing = 4,
            NSReorg = 5
        }
    }

    public class SimpleLayerParser : LayerParser
    {
        public SimpleLayerParser(string layername) : base(layername) { }
        public override void Push()
        {
            //Console.WriteLine(OutputLayerName);
        }
    }

    internal class CurLayerParser : LayerParser
    {
        internal CurLayerParser() : base(Clayername()) { ActiveLayerParsers.Add(this); }

        private static string Clayername()
        {
            Workstation.Define(out Database db);
            LayerTableRecord ltr = (LayerTableRecord)db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead);
            return ltr.Name;
        }

        public override void Push()
        {
            Workstation.Define(out Teigha.DatabaseServices.TransactionManager tm);
            Workstation.Define(out Database db);

            LayerChecker.Check(OutputLayerName);
            LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForRead);
            var elem = from ObjectId layer in lt
                       let ltr = (LayerTableRecord)tm.GetObject(layer, OpenMode.ForRead)
                       where ltr.Name == OutputLayerName
                       select layer;
            db.Clayer = elem.FirstOrDefault();
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName,out bool propsgetsuccess);
            db.Celtscale = lp.LTScale;
            db.Plinewid = lp.ConstWidth;
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
            ObjList.Add(ent); ActiveLayerParsers.Add(this);
        }

        internal List<Entity> ObjList = new List<Entity>();
        public override void Push()
        {
            LayerChecker.Check(OutputLayerName);
            LayerProps lp = LayerPropertiesDictionary.GetValue(OutputLayerName, out bool propsgetsuccess);
            foreach (Entity ent in ObjList)
            {
                ent.Layer = OutputLayerName;
                if (ent is Polyline pl)
                {
                    pl.LinetypeScale=lp.LTScale;
                    pl.ConstantWidth = lp.ConstWidth;
                }
            }
        }
    }

    internal class RecordLayerParser : LayerParser
    {
        const byte redproj = 0; const byte greenproj = 255; const byte blueproj = 255;
        const byte redns = 0; const byte greenns = 153; const byte bluens = 153;
        internal LayerTableRecord BoundLayer;
        internal bool StoredEnabledState;
        internal Teigha.Colors.Color StoredColor;
        internal RecordLayerParser(LayerTableRecord ltr) : base(ltr.Name)
        {
            BoundLayer = ltr;
            StoredEnabledState = ltr.IsOff;
            StoredColor = ltr.Color;
            ChapterStoredRecordLayerParsers.Add(this);
        }
        public void Reset()
        {
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
                    if (base.bldstatus == (int)Status.Planned)
                    {
                        BoundLayer.Color = Teigha.Colors.Color.FromRgb(redproj, greenproj, blueproj);
                    }
                    else if (base.bldstatus == (int)Status.NSPlanned)
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
        internal static void StatusSwitch(LayerParser.Status status)
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
        private static Dictionary<Document, bool> eventassigned = new Dictionary<Document, bool>(); //должно работать только для одного документа. переделать для многих
        internal static Dictionary<Document, List<RecordLayerParser>> List { get; } = new Dictionary<Document, List<RecordLayerParser>>();
        internal static void Add(RecordLayerParser lp)
        {
            Workstation.Define(out Document doc);
            if (!List.ContainsKey(doc))
            {
                List[doc] = new List<RecordLayerParser>();
                eventassigned.Add(doc, false);
            }
            if (!List[doc].Any(l => l.InputLayerName == lp.InputLayerName))
            {
                List[doc].Add(lp);
            }
        }
        internal static void Reset()
        {
            Workstation.Define(out Document doc);
            foreach (RecordLayerParser lp in List[doc]) { lp.Reset(); }
            doc.Database.BeginSave -= Reset;
            eventassigned[doc] = false;
        }

        internal static void Reset(object sender, EventArgs e)
        {
            Workstation.Define(out Document doc);
            Workstation.Define(out Teigha.DatabaseServices.TransactionManager tm);

            using (Transaction myT = tm.StartTransaction())
            {
                foreach (RecordLayerParser lp in List[doc])
                {
                    LayerTableRecord ltr = (LayerTableRecord)tm.GetObject(lp.BoundLayer.Id, OpenMode.ForWrite);
                    lp.Reset();
                }

                doc.Database.BeginSave -= Reset;
                eventassigned[doc] = false;
                ChapterVisualizer.ActiveChapterState[doc] = null;
                Flush();
                myT.Commit();
            }

        }
        internal static void Highlight(string engtype)
        {
            Workstation.Define(out Document doc);
            if (!eventassigned[doc])
            {
                doc.Database.BeginSave += Reset;
                eventassigned[doc] = true;
            }
            foreach (RecordLayerParser lp in List[doc]) { lp.Push(engtype); }
        }
        internal static void Flush()
        {
            Workstation.Define(out Document doc);
            List[doc].Clear();
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


