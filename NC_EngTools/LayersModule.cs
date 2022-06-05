using System.Linq;

namespace LayerProcessing
{
    using System;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using Teigha.DatabaseServices;
    using LayerPropsExtraction;
    using NC_EngTools;
    public abstract class LayerParser
    {
        public static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "неутв", "ндем", "нреорг" };
        public string InputLayerName { get; private set; }
        public string OutputLayerName { get; private set; }

        private bool recstatus = false;
        private bool extpr = false;
        private string engtype;
        private string extprojectname;
        private int bldstatus;
        public LayerParser(string layername)
        {
            InputLayerName = layername;
            if (!layername.StartsWith(StandartPrefix))
            {
                throw new WrongLayerException($"Слой {layername} не обрабатывается программой");
                //обработать при передаче слоёв
            }

            string[] decomp = InputLayerName.Split('_');
            //searching for external project name enclosed in [] and storing it
            if (decomp[1].StartsWith("["))
            {
                extpr = true;
                Regex rgx = new Regex(@"\[(\w*)\]");
                string mtch1 = rgx.Match(InputLayerName).ToString().Replace("[", "").Replace("]", "");
                extprojectname = mtch1;
            }
            //searching for reconstruction status marker "_пер"
            if (decomp[decomp.Length-1]=="пер") { recstatus=true; }
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

        private string trimmedname
        {
            get
            {
                string s = OutputLayerName;
                Regex rgx = new Regex(@"(_"+string.Join("|_", st_txt)+")(\b_|$)");
                s = rgx.Replace(s, "");
                rgx = new Regex(StandartPrefix+"_");
                s = rgx.Replace(s, "");
                if (extpr)
                {
                    rgx = new Regex(@"\[(\w*)\]_");
                    s = rgx.Replace(s, "");
                }
                if (recstatus)
                {
                    rgx = new Regex("_пер");
                    s = rgx.Replace(s, "");
                }
                return s;
            }
        }
        public string TrueName
        {
            get { return string.Join("_", new string[2] { trimmedname, st_txt[bldstatus] }); }
        }

        public void StatusSwitch(Status newstatus)
        {
            string rgxpattern = @"(_"+string.Join("|_", st_txt)+")(\b_|$)";
            Regex rgx = new Regex(rgxpattern);
            bldstatus=(int)newstatus;
            OutputLayerName = rgx.Replace(OutputLayerName, "_"+st_txt[(int)newstatus]);
            //disabling reconstruction marker for existing objects layers
            if (recstatus & !(bldstatus==2||bldstatus==3||bldstatus==5))
            {
                ReconstrSwitch();
            }
            //disabling external project tag for current project and existing layers
            if (extpr & !(bldstatus==3||bldstatus==4||bldstatus==5))
            {
                rgx = new Regex(@"_\[(\w*)\]");
                OutputLayerName = rgx.Replace(OutputLayerName, "");
                extprojectname = ""; extpr = false;
            }
            return;
        }

        public void ReconstrSwitch()
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

        public void ExtProjNameAssign(string newprojname)
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
                Regex rgx = new Regex(@".._.._");
                string repl = StandartPrefix+"_["+newprojname+"]_"+engtype+"_";
                OutputLayerName=rgx.Replace(OutputLayerName, repl);
                extpr = true;
            }
        }

        public void Alter()
        {
            throw new NotImplementedException();
        }

        public abstract void Push();

        public enum Status
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

    public class CurLayerParser : LayerParser
    {
        public CurLayerParser(Database db) : base(clayername(db)) { Db = db; ActiveLayerParsers.Add(this); }

        private static string clayername(Database db)
        {
            LayerTableRecord ltr = (LayerTableRecord)db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead);
            return ltr.Name;
        }
        private Database Db;
        
        public override void Push()
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
    public class EntityLayerParser : LayerParser
    {
        public EntityLayerParser(string layername) : base(layername) { ActiveLayerParsers.Add(this); }
        public EntityLayerParser(string layername, Transaction transaction) : base(layername)
        {
            Transaction = transaction;
            ActiveLayerParsers.Add(this);
        }
        public EntityLayerParser(Entity ent) : base(ent.Layer) { ObjList.Add(ent); ActiveLayerParsers.Add(this); }
        public EntityLayerParser(Entity ent, Transaction transaction) : base(ent.Layer) 
        { 
            ObjList.Add(ent);
            Transaction = transaction;
            ActiveLayerParsers.Add(this);
        }
        
        public List<Entity> ObjList = new List<Entity>();
        public Transaction Transaction { get; private set; }
        public override void Push()
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
                    if (ent is Polyline)
                    {
                        Polyline pl = (Polyline)ent;
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

    public static class ActiveLayerParsers
    {
        private static List<LayerParser> List { get; set; } = new List<LayerParser>();
        public static void StatusSwitch(LayerParser.Status status)
        {
            foreach (LayerParser lp in List) { lp.StatusSwitch(status); };
        }
        public static void Alter()
        {
            foreach (LayerParser lp in List) { lp.Alter(); }
        }
        public static void ReconstrSwitch()
        {
            foreach (LayerParser lp in List) { lp.ReconstrSwitch(); }
        }
        public static void ExtProjNameAssign(string extprojname)
        {
            foreach (LayerParser lp in List) { lp.ExtProjNameAssign(extprojname); }
        }
        public static void Add(LayerParser lp)
        {
            List.Add(lp);
        }
        public static void Push()
        {
            foreach (LayerParser lp in List) { lp.Push(); }
        }
        public static void Flush()
        {
            List.Clear();
        }
    }
    public class WrongLayerException : System.Exception
    {
        public WrongLayerException(string message) : base(message) { }
    }
    public class NoPropertiesException : System.Exception
    {
        public NoPropertiesException(string message) : base(message) { }
    }
}


