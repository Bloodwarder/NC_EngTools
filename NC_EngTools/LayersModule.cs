using System.Linq;


namespace LayerProcessing
{
    using System;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using Teigha.DatabaseServices;
    using ExternalData;
    using NC_EngTools;
    public abstract class LayerParser
    {
        internal protected static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "неутв", "ндем", "нреорг" };
        internal protected string InputLayerName { get; private set; }
        internal protected string OutputLayerName { get; private set; }
        internal protected string MainName { get; set; }
        internal protected string TrueName
        //string for props compare (type of network and it's status)
        {
            get { return string.Join("_", new string[2] { MainName, st_txt[bldstatus] }); }
        }

        private bool recstatus = false;
        private bool extpr = false;
        private string engtype;
        private string extprojectname;
        private int bldstatus;
        internal protected LayerParser(string layername)
        {
            InputLayerName = layername;
            if (!layername.StartsWith(StandartPrefix))
            {
                throw new WrongLayerException($"Слой {layername} не обрабатывается программой");
                //обработать при передаче слоёв
            }
            int mainnamestart;
            int mainnameend;
            string[] decomp = InputLayerName.Split('_');
            //searching for external project name enclosed in [] and storing it
            if (decomp[1].StartsWith("["))
            {
                mainnamestart = 2;
                extpr = true;
                Regex rgx = new Regex(@"\[(\w*)\]");
                string mtch1 = rgx.Match(InputLayerName).ToString().Replace("[", "").Replace("]", "");
                extprojectname = mtch1;
            }
            else
            {
                mainnamestart = 1;
            }
            //searching for reconstruction status marker "_пер"
            if (decomp[decomp.Length-1]=="пер") 
            { 
                recstatus=true;
                mainnameend = decomp.Length-3;
            }
            else
            {
                mainnameend = decomp.Length-2;
            }
            //assigning main name containing main type information (type of network, presented by layer)
            MainName = string.Join("_",decomp.Skip(mainnamestart).Take(mainnameend-mainnamestart+1));
            //searching for status in last or last-1 position depending of recstatus
            string str = recstatus ? decomp[decomp.Length-2] : decomp[decomp.Length-1];
            bool stfound = false;
            for (int i = 0; i<st_txt.Length; i++) { if (st_txt[i]==str) { bldstatus=i; stfound = true; break; } } //searching and assigning status index (IMPROVE CODE LATER)
            if (!stfound) { throw new WrongLayerException($"В слое {layername} не найден статус"); }
            //searching for network type in second position or behind the external project name
            if (extpr)
            {
                int typeidx = 2;

                for (int i = 1; i<decomp.Length; i++)
                {
                    if (decomp[i].EndsWith("]")) { typeidx=i+1; break; }
                }
                try
                {
                    engtype=decomp[typeidx];
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new WrongLayerException($"Ошибочное имя слоя {layername}. Символ ] в последнем блоке имени слоя");
                }
            }
            else
            {
                engtype=decomp[1];
            }
            OutputLayerName = InputLayerName;
        }

        internal protected void StatusSwitch(Status newstatus)
        {
            bldstatus=(int)newstatus;


            //disabling reconstruction marker for existing objects layers
            if (recstatus & !(bldstatus==2||bldstatus==3||bldstatus==5))
            {
                ReconstrSwitch();
            }
            //disabling external project tag for current project and existing layers
            if (extpr & !(bldstatus==3||bldstatus==4||bldstatus==5))
            {
                Regex rgx = new Regex(@"_\[(\w*)\]");
                OutputLayerName = rgx.Replace(OutputLayerName, "");
                extprojectname = ""; extpr = false;
            }
            Regex rgx1 = new Regex(@"(_"+string.Join("|_", st_txt)+")(\b_|$)"); 
            OutputLayerName = rgx1.Replace(OutputLayerName, "_"+st_txt[(int)newstatus]);
            return;
        }

        internal protected void ReconstrSwitch()
        {
            if (bldstatus==2||bldstatus==3||bldstatus==5) //filter only planned layers
            {
                if (recstatus)
                {
                    Regex rgx = new Regex("_пер$");
                    OutputLayerName = rgx.Replace(OutputLayerName, "");
                    recstatus=false;
                    return;
                }
                else
                {
                    OutputLayerName += "_пер";
                    recstatus=true;
                    return;
                }
            }
        }

        internal protected void ExtProjNameAssign(string newprojname)
        {
            if (newprojname==extprojectname||!(bldstatus==3||bldstatus==4||bldstatus==5)) { return; } //stop when entry=current value or current project layer processed (non NS)
            if (extpr)
            {
                if (newprojname!="") //replacing name
                {
                    extprojectname= newprojname;
                    Regex rgx = new Regex(@"\[(\w*)\]");
                    string repl = "["+newprojname+"]";
                    OutputLayerName = rgx.Replace(OutputLayerName, repl);
                }
                else //erasing name
                {
                    Regex rgx = new Regex(@"_\[(\w*)\]_");
                    string repl = "_";
                    OutputLayerName = rgx.Replace(OutputLayerName, repl);
                    extprojectname = "";
                    extpr = false;
                }
            }
            else //assigning name
            {
                extprojectname = newprojname;
                Regex rgx = new Regex(@"$(\w*)_.._");
                string repl = StandartPrefix+"_["+newprojname+"]_"+engtype+"_";
                OutputLayerName=rgx.Replace(OutputLayerName, repl);
                extpr = true;
            }
        }

        internal protected void Alter()
        {
            bool success = LayerAlteringDictionary.Dictionary.TryGetValue(MainName, out string str);
            if (!success) { return; }
            Regex rgx = new Regex(MainName);
            OutputLayerName = rgx.Replace(OutputLayerName, str);
            MainName = str;
        }

        internal protected abstract void Push();

        internal protected enum Status
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
        internal protected override void Push()
        {
            //Console.WriteLine(OutputLayerName);
        }
    }

    internal class CurLayerParser : LayerParser
    {
        internal CurLayerParser(Database db) : base(Clayername(db)) { Db = db; ActiveLayerParsers.Add(this); }

        private static string Clayername(Database db)
        {
            LayerTableRecord ltr = (LayerTableRecord)db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead);
            return ltr.Name;
        }
        private Database Db;

        internal protected override void Push()
        {
            LayerChecker.Check(OutputLayerName);
            LayerTable lt = (LayerTable) Db.TransactionManager.GetObject(Db.LayerTableId, OpenMode.ForRead);
            bool lfound = false;
            foreach (var (elem, ltr) in from ObjectId elem in lt
                                        let ltr = (LayerTableRecord)Db.TransactionManager.GetObject(elem, OpenMode.ForRead)
                                        select (elem, ltr))
            {
                if (ltr.Name==OutputLayerName)
                {
                    Db.Clayer = elem;
                    lfound = true;
                    break;
                }
            }
            if (!lfound) { throw new Exception(); }
        }
    }
    internal class EntityLayerParser : LayerParser
    {
        internal EntityLayerParser(string layername) : base(layername) { ActiveLayerParsers.Add(this); }
        internal EntityLayerParser(string layername, Transaction transaction) : base(layername)
        {
            Transaction = transaction;
            ActiveLayerParsers.Add(this);
        }
        internal EntityLayerParser(Entity ent) : base(ent.Layer) { ObjList.Add(ent); ActiveLayerParsers.Add(this); }
        internal EntityLayerParser(Entity ent, Transaction transaction) : base(ent.Layer) 
        { 
            ObjList.Add(ent);
            Transaction = transaction;
            ActiveLayerParsers.Add(this);
        }

        internal List<Entity> ObjList = new List<Entity>();
        internal Transaction Transaction { get; private set; }
        internal protected override void Push()
        {
            try
            {
                LayerChecker.Check(OutputLayerName);
                bool success = LayerProperties.Dictionary.TryGetValue(TrueName, out LayerProps lp);
                if (!success)
                { lp = new LayerProps() { ConstWidth = 0.4, LineWeight = -3, LTScale=0.8, Red=0, Green=0, Blue=0, LTName = "Continious"}; }
                foreach (Entity ent in ObjList)
                {
                    ent.Layer = OutputLayerName;
                    if (ent is Polyline pl)
                    {
                        pl.LinetypeScale=lp.LTScale;
                        pl.ConstantWidth = lp.ConstWidth;
                    }
                }
                //transaction is committed outside this class
            }
            catch(NoPropertiesException)
            {
                return;
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
    internal class WrongLayerException : System.Exception
    {
        public WrongLayerException(string message) : base(message) { }
    }
    internal class NoPropertiesException : System.Exception
    {
        public NoPropertiesException(string message) : base(message) { }
    }
}


