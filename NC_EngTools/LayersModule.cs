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
        public string InputLayerName { get; private set; }
        public static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
        public string ExtProjectName { get; private set; }
        public string EngType { get; private set; }
        public string GeomType { get; private set; }
        public string MainName { get; private set; }
        public string BuildStatus { get { return st_txt[bldstatus]; } }
        public string OutputLayerName 
        { get
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
                //Regex rgx = new Regex($"_{GeomType}_");
                //string mainwgeom = string.Join("_", decomp.Skip(mainnamestart).Take(mainnameend-mainnamestart+1));
                //MainName = rgx.Replace(mainwgeom, "");
                return string.Join("_", new string[2] { MainName, st_txt[bldstatus] }); 
            }
        }
        private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "неутв", "ндем", "нреорг" };

        private bool recstatus = false;
        private bool extpr = false;
        private bool geomassigned = false;
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
            int mainnamestart=1;
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
                counter =+ list.Count + 1;
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
                    return;
                }
                else
                {
                    recstatus=true;
                    return;
                }
            }
        }

        public void ExtProjNameAssign(string newprojname)
        {
            if (newprojname!=""&(bldstatus==3||bldstatus==4||bldstatus==5))
            {
                bldstatus = 3; 
            } //assigning NSPlanned status when current project layer processed (non NS)
            if (extpr)
            {
                if (newprojname!="") //replacing name
                {
                    ExtProjectName= newprojname;
                }
                else //erasing name
                {
                    ExtProjectName = "";
                    extpr = false;
                }
            }
            else //assigning name
            {
                ExtProjectName = newprojname;
                extpr = true;
            }
        }

        public void Alter()
        {
            bool success = LayerAlteringDictionary.Dictionary.TryGetValue(MainName, out string str);
            if (!success) { return; }
            MainName = str;
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

    internal class CurLayerParser : LayerParser
    {
        internal CurLayerParser(Database db) : base(Clayername(db)) { Db = db; ActiveLayerParsers.Add(this); }

        private static string Clayername(Database db)
        {
            LayerTableRecord ltr = (LayerTableRecord)db.TransactionManager.GetObject(db.Clayer, OpenMode.ForRead);
            return ltr.Name;
        }
        private Database Db;

        public override void Push()
        {
            LayerChecker.Check(OutputLayerName);
            LayerTable lt = (LayerTable) Db.TransactionManager.GetObject(Db.LayerTableId, OpenMode.ForRead);
            var elem = from ObjectId layer in lt
                            let ltr = (LayerTableRecord)Db.TransactionManager.GetObject(layer, OpenMode.ForRead)
                            where ltr.Name == OutputLayerName
                            select layer;
            Db.Clayer = elem.FirstOrDefault();
            bool success = LayerProperties.Dictionary.TryGetValue(TrueName, out LayerProps lp);
            if(success)
            {
                Db.Celtscale = lp.LTScale;
                Db.Plinewid = lp.ConstWidth;
            }
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


