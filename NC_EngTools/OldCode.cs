using LayersModule; 

public abstract class OldLayerParser
{
    public static string StandartPrefix { get; set; } = "ИС"; // ConfigurationManager.AppSettings.Get("defaultlayerprefix");
    private static readonly string[] st_txt = new string[6] { "сущ", "дем", "пр", "неутв", "ндем", "нреорг" };
    public string InputLayerName { get; private set; }
    public string OutputLayerName { get; private set; }
    public string MainName { get; set; }
    public string TrueName
    //string for props compare (type of network and it's status)
    {
        get { return string.Join("_", new string[2] { MainName, st_txt[bldstatus] }); }
    }

    private bool recstatus = false;
    private bool extpr = false;
    private string engtype;
    private string extprojectname;
    private int bldstatus;
    public OldLayerParser(string layername)
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
        MainName = string.Join("_", decomp.Skip(mainnamestart).Take(mainnameend-mainnamestart+1));
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

    public void StatusSwitch(Status newstatus)
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
            Regex rgx = new Regex($"^{StandartPrefix}_{engtype}_");
            string repl = StandartPrefix+"_["+newprojname+"]_"+engtype+"_";
            OutputLayerName=rgx.Replace(OutputLayerName, repl);
            extpr = true;
        }
    }

    public void Alter()
    {
        bool success = LayerAlteringDictionary.Dictionary.TryGetValue(MainName, out string str);
        if (!success) { return; }
        Regex rgx = new Regex(MainName);
        OutputLayerName = rgx.Replace(OutputLayerName, str);
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
