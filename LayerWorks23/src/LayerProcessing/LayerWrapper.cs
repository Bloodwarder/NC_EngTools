using LayersIO.ExternalData;
using LayerWorks23.LayerProcessing;
using System.Text.RegularExpressions;

namespace LayerWorks.LayerProcessing
{
    /// <summary>
    /// Парсер слоя, получающий информацию об объекте на основе имени
    /// </summary>
    public abstract class LayerWrapper
    {
        /// <summary>
        /// Имя исходного слоя, переданного в конструктор 
        /// </summary>
        public string InputLayerName { get; private set; }
        /// <summary>
        /// Стандартный префикс имени слоя для фильтрации слоёв, которые парсер будет пытаться обработать
        /// </summary>
        public static string StandartPrefix { get; set; } = "ИС";
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
        public LayerWrapper(string layername)
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
            if (decomp[^1] == "пер")
            {
                recstatus = true;
                mainnameend = decomp.Length - 3;
            }
            else
            {
                mainnameend = decomp.Length - 2;
            }
            string str = recstatus ? decomp[^2] : decomp[^1];
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

    public abstract class LayerWrapperNew
    {
        public LayerInfo LayerInfo { get; private set; }
        public LayerWrapperNew(string layerName)
        {
            // Поиск префикса по любому возможному разделителю
            string prefix = Regex.Match(layerName, @"^[^_\s-\.]+(?=[_\s-\.])").Value;
            LayerInfo = NameParser.LoadedParsers[prefix].GetLayerInfo(layerName);

        }
        public abstract void Push();
    }
}


