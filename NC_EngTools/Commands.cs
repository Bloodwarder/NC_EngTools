﻿//System
using System.Linq;
using System.Collections.Generic;
using System.IO;
//Modules
using LayerProcessing;
using ExternalData;
using Dictionaries;
//nanoCAD
using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Platform = HostMgd;
using PlatformDb = Teigha;


namespace NC_EngTools
{
    public class NCLayersCommands
    {
        public static string PrevStatus = "Сущ";
        public static string PrevExtProject = "";
        [CommandMethod("КАЛЬКА")]
        public void TToggle()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;

            string tgtlayer = LayerParser.StandartPrefix+"_Калька";

            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForWrite, false);
                    if (!lt.Has(tgtlayer))
                    {
                        LayerTableRecord ltrec = new LayerTableRecord
                        {
                            Name = tgtlayer,
                            Color = PlatformDb.Colors.Color.FromRgb(255, 255, 255),
                            //Transparency = new PlatformDb.Colors.Transparency(PlatformDb.Colors.TransparencyMethod.ByAlpha)
                        };
                        lt.Add(ltrec);
                        myT.AddNewlyCreatedDBObject(ltrec, true);
                    }
                    else
                    {
                        LayerTableRecord ltrec = (from ObjectId elem in lt
                                                  let ltr = (LayerTableRecord)tm.GetObject(elem, OpenMode.ForWrite, false)
                                                  where ltr.Name == tgtlayer
                                                  select ltr)
                                                  .FirstOrDefault();
                        if (ltrec.IsFrozen||ltrec.IsOff)
                        {
                            ltrec.IsOff = false;
                            ltrec.IsFrozen = false;
                        }
                        else
                        {
                            ltrec.IsOff = true;
                        }

                    }
                    myT.Commit();
                }
                finally { myT.Dispose(); }
            }
        }

        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public void LayerSC()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);

            PromptKeywordOptions pko = new PromptKeywordOptions($"Укажите статус объекта <{PrevStatus}> [Сущ/Демонтаж/Проект/Неутв/Неутв_демонтаж/Неутв_реорганизация]", "Сущ Демонтаж Проект Неутв Неутв_демонтаж Неутв_реорганизация")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult res = ed.GetKeywords(pko);
            if (res.Status == PromptStatus.OK) { PrevStatus = res.StringResult; }
            StatusTextDictionary.StTxtDictionary.TryGetValue(res.StringResult, out int val);
            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.StatusSwitch((LayerParser.Status)val);
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }
        }

        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public void LayerAlter()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);

            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Alter();
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }
        }

        [CommandMethod("ПЕРЕУСТРОЙСТВО", CommandFlags.Redraw)]
        public void LayerReconstr()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);

            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.ReconstrSwitch();
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }
        }

        [CommandMethod("ВНЕШПРОЕКТ", CommandFlags.Redraw)]
        public void ExtAssign()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);

            PromptResult pr = ed.GetString($"Введите имя проекта, согласно которому отображён выбранный объект <{PrevExtProject}>");
            string extprname;
            if (pr.Status != (PromptStatus.Error | PromptStatus.Cancel))
            {
                extprname = pr.StringResult;
                PrevExtProject = extprname;
            }
            else
            {
                return;
            }
            
            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.ExtProjNameAssign(extprname);
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }
        }

        [CommandMethod("СВС", CommandFlags.Redraw)]
        public void StandartLayerValues()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);

            using (Transaction myT = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }

        }

        [CommandMethod("ПРЕФИКС")]
        public void ChangePrefix()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptStringOptions pso = new PromptStringOptions($"Введите новый префикс обрабатываемых слоёв <{LayerParser.StandartPrefix}>")
            {
                AllowSpaces = false
            };
            string newprefix = ed.GetString(pso).StringResult;
            if (!string.IsNullOrEmpty(newprefix))
            {
                LayerParser.StandartPrefix = newprefix;
            }

        }
    }
    public class ChapterVisualizer
    {
        private static Dictionary<Document, string> activeChapterState = new Dictionary<Document, string>();

        internal static Dictionary<Document, string> ActiveChapterState
        {
            get
            {
                Workstation.Define(out Document doc);
                if (!activeChapterState.ContainsKey(doc))
                { activeChapterState.Add(doc, null); }
                return activeChapterState;
            }
        }
        [CommandMethod("ВИЗРАЗДЕЛ")]
        public void Visualizer()
        {
            Workstation.Define(out Document doc, out Database db, out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);
            using (Transaction myT = tm.StartTransaction())
            {
                LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForRead, false);
                var layers = from ObjectId elem in lt
                             let ltr = (LayerTableRecord)tm.GetObject(elem, OpenMode.ForWrite, false)
                             where ltr.Name.StartsWith(LayerParser.StandartPrefix+"_")
                             select ltr;
                foreach (LayerTableRecord ltr in layers)
                {
                    try
                    {
                        new RecordLayerParser(ltr);
                    }
                    catch (WrongLayerException ex)
                    {
                        ed.WriteMessage(ex.Message);
                        continue;
                    }
                }
                var layerchapters = ChapterStoredRecordLayerParsers.List[doc].Where(l => l.EngType!=null).Select(l => l.EngType).Distinct().OrderBy(l => l).ToList();
                List<string> lcplus = layerchapters.Append("Сброс").ToList();
                PromptKeywordOptions pko = new PromptKeywordOptions($"Выберите раздел ["+string.Join("/", lcplus)+"]", string.Join(" ", lcplus))
                {
                    AppendKeywordsToMessage = true,
                    AllowNone = false,
                    AllowArbitraryInput = false
                };
                PromptResult res = ed.GetKeywords(pko);
                if (res.Status != PromptStatus.OK) { return; }
                if (res.StringResult == "Сброс")
                {
                    ChapterStoredRecordLayerParsers.Reset();
                    if (ActiveChapterState!=null)
                    {
                        LayerChecker.LayerAdded -= NewLayerHighlight;
                        ActiveChapterState[doc] = null;
                    }

                }
                else
                {
                    ActiveChapterState[doc] = res.StringResult;
                    ChapterStoredRecordLayerParsers.Highlight(ActiveChapterState[doc]);
                    LayerChecker.LayerAdded += NewLayerHighlight;
                }
                myT.Commit();
            }
        }

        public void NewLayerHighlight(object sender, System.EventArgs e)
        {
            Workstation.Define(out Document doc, out Database db, out Teigha.DatabaseServices.TransactionManager tm);
            using (Transaction myT = tm.StartTransaction())
            {
                RecordLayerParser rlp = new RecordLayerParser((LayerTableRecord)sender);
                rlp.Push(ActiveChapterState[doc]);
                myT.Commit();
            }

        }
    }





    static class LayerChanger
    {
        internal static int MaxSimple { get; set; } = 5;

        internal static void UpdateActiveLayerParsers()
        {
            Workstation.Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed);
            PromptSelectionResult sr = ed.SelectImplied();

            if (sr.Status == PromptStatus.OK)
            {
                SelectionSet ss = sr.Value;
                if (ss.Count<MaxSimple)
                {
                    ChangerSimple(tm, ss);
                }
                else
                {
                    ChangerBig(tm, ss);
                }
            }
            else
            {
                new CurLayerParser();
            }
        }

        private static void ChangerSimple(PlatformDb.DatabaseServices.TransactionManager tm, SelectionSet ss)
        {
            foreach (Entity ent in from ObjectId elem in ss.GetObjectIds()
                                   let ent = (Entity)tm.GetObject(elem, OpenMode.ForWrite)
                                   select ent)
            {
                try
                {
                    EntityLayerParser entlp = new EntityLayerParser(ent);
                }
                catch (WrongLayerException)
                {
                    continue;
                }
            }
        }

        private static void ChangerBig(PlatformDb.DatabaseServices.TransactionManager tm, SelectionSet ss)
        {
            Dictionary<string, EntityLayerParser> dct = new Dictionary<string, EntityLayerParser>();
            foreach (var ent in from ObjectId elem in ss.GetObjectIds()
                                let ent = (Entity)tm.GetObject(elem, OpenMode.ForWrite)
                                select ent)
            {
                if (dct.ContainsKey(ent.Layer))
                {
                    dct[ent.Layer].ObjList.Add(ent);
                }
                else
                {
                    try
                    {
                        dct.Add(ent.Layer, new EntityLayerParser(ent));
                    }
                    catch (WrongLayerException)
                    {
                        continue;
                    }
                }
            }
        }
    }

    static class LayerChecker
    {
        internal static event System.EventHandler LayerAdded;
        internal static void Check(string layername)
        {

            Database db = HostApplicationServices.WorkingDatabase;
            PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;
            Transaction transaction = tm.StartTransaction();

            using (transaction)
            {
                try
                {
                    LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForWrite, false);
                    if (!lt.Has(layername))
                    {
                        LayerProps lp = LayerPropertiesDictionary.GetValue(layername,out bool propsgetsuccess);
                        LinetypeTable ltt = (LinetypeTable)tm.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false);
                        bool ltgetsucess = true;
                        if (!ltt.Has(lp.LTName))
                        {
                            FileInfo fi = new FileInfo(PathOrganizer.GetPath("Linetypes"));
                            try
                            { db.LoadLineTypeFile(lp.LTName, fi.FullName); }
                            catch
                            { ltgetsucess = false; }
                        }
                        ObjectId lttrId = SymbolUtilityServices.GetLinetypeContinuousId(db);
                        if (ltgetsucess)
                        {
                            var elem = from ObjectId layer in ltt
                                       let lttr = (LinetypeTableRecord)tm.GetObject(layer, OpenMode.ForRead)
                                       where lttr.Name.ToUpper()==lp.LTName.ToUpper()
                                       select lttr;
                            lttrId = elem.FirstOrDefault().Id;
                        }
                        else
                        {
                            string str = $"Не найден тип линий для слоя {layername}";
                            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(str);
                        }
                        LayerTableRecord ltrec = new LayerTableRecord
                        {
                            Name = layername,
                            Color = PlatformDb.Colors.Color.FromRgb(lp.Red, lp.Green, lp.Blue),
                            LineWeight = (LineWeight)lp.LineWeight,
                            LinetypeObjectId = lttrId
                            //Transparency = new PlatformDb.Colors.Transparency(PlatformDb.Colors.TransparencyMethod.ByAlpha)
                        };
                        lt.Add(ltrec);
                        tm.AddNewlyCreatedDBObject(ltrec, true);
                        //Process new layer if isolated chapter visualization is active

                        System.EventArgs e = new System.EventArgs();
                        transaction.Commit();
                        if (LayerAdded!=null)
                        {
                            LayerAdded(ltrec, e);
                        }

                    }
                    else
                    {
                        return;
                    }
                }
                catch (NoPropertiesException)
                {
                    throw new NoPropertiesException("Проверка слоя не удалась");
                }
                finally
                {
                    transaction.Dispose();
                }
            }

        }
    }

    static class Workstation
    {
        internal static void Define(out Document doc)
        {
            doc = Application.DocumentManager.MdiActiveDocument;
        }
        internal static void Define(out Database db)
        {
            db = HostApplicationServices.WorkingDatabase;
        }
        internal static void Define(out Document doc, out Database db)
        {
            Define(out doc);
            Define(out db);
        }
        internal static void Define(out Document doc, out Database db, out PlatformDb.DatabaseServices.TransactionManager tm)
        {
            Define(out doc, out db);
            tm = db.TransactionManager;
        }
        internal static void Define(out PlatformDb.DatabaseServices.TransactionManager tm)
        {
            Define(out Database db);
            tm = db.TransactionManager;
        }
        internal static void Define(out Document doc, out Database db, out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed)
        {
            Define(out doc, out db, out tm);
            ed = doc.Editor;
        }
        internal static void Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed)
        {
            Define(out Document doc);
            Define(out tm);
            ed = doc.Editor;
        }

    }
}


namespace TestCommands
{
    public class TEST_12
    {

        [CommandMethod("TEST_12")]
        public void Template2()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Document doc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;

            // Выводим в командную строку информацию о первых 10 слоях
            ed.WriteMessage("Выводим первые 10 слоев:");
            using (Transaction myT = tm.StartTransaction())
            {
                LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForRead, false);
                LayerTableRecord ltrec;

                SymbolTableEnumerator lte = lt.GetEnumerator();
                for (int i = 0; i < 10; ++i)
                {
                    if (!lte.MoveNext())
                    {
                        break;
                    }

                    ObjectId id = (ObjectId)lte.Current;
                    ltrec = (LayerTableRecord)tm.GetObject(id, OpenMode.ForRead);
                    ed.WriteMessage(string.Format("Имя слоя:{0}; Цвет слоя: {1}; Код слоя:{2}; Прозрачность: {3}", ltrec.Name, ltrec.Color.ToString(), ltrec.Description, ltrec.Transparency.ToString()));
                }
            }

            PromptStringOptions opts = new PromptStringOptions("Введите имя нового слоя")
            {
                AllowSpaces = true
            };
            PromptResult pr = ed.GetString(opts);
            if (PromptStatus.OK == pr.Status)
            {
                string newLayerName = pr.StringResult;

                // Создаем новый слой
                using (Transaction myT = tm.StartTransaction())
                {
                    try
                    {
                        LayerTable lt = (LayerTable)tm.GetObject(db.LayerTableId, OpenMode.ForWrite, false);

                        // Проверяем есть ли такой слой
                        if (!lt.Has(newLayerName))
                        {
                            LayerTableRecord ltrec = new LayerTableRecord
                            {
                                Name = newLayerName
                            };
                            lt.Add(ltrec);
                            tm.AddNewlyCreatedDBObject(ltrec, true);
                            myT.Commit();
                        }
                    }
                    finally
                    {
                        myT.Dispose();
                    }
                }
            }
            else
            {
                ed.WriteMessage("Отмена.");
            }
        }
    }
}




