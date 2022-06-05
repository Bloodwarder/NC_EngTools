using System.Linq;

namespace NC_EngTools
{
    using LayerProcessing;
    using LayerPropsExtraction;
    using HostMgd.ApplicationServices;
    using HostMgd.EditorInput;
    using Teigha.DatabaseServices;
    using Teigha.Runtime;
    using Platform = HostMgd;
    using PlatformDb = Teigha;
    using System.Collections.Generic;

    using Dictionaries;

    public class NCCommandsLayers
    {
        public static string PrevStatus = "Сущ";
        [CommandMethod("TTOGGLE")]
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
                        myT.Commit();
                    }
                    else
                    {
                        foreach (ObjectId elem in lt)
                        {
                            LayerTableRecord ltrec = (LayerTableRecord)tm.GetObject(elem, OpenMode.ForWrite);
                            if (ltrec.Name == tgtlayer)
                            {
                                ltrec.IsOff = !ltrec.IsOff;
                                myT.Commit();
                                break;
                            }
                        }
                    }
                }
                finally { myT.Dispose(); }
            }
        }

        [CommandMethod("СТАТУСИЗМ",CommandFlags.UsePickSet)]
        public void LayerSC()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = HostApplicationServices.WorkingDatabase;
            PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;

            Editor ed = doc.Editor;
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
                    LayerChanger.UpdateActiveLP(doc, db, myT); //незачем передавать транзацкцию. если внутри что-то не так, её и так очистит ИСПРАВИТЬ!
                    ActiveLayerParsers.StatusSwitch((LayerParser.Status)val);
                    ActiveLayerParsers.Push();
                    myT.Commit();
                }
                finally
                {
                    myT.Dispose();
                    ActiveLayerParsers.Flush();
                }
            }
                
        }
    }
    public static class LayerChanger
    {
        public static int maxsimple { get; set; } = 15;

        public static void UpdateActiveLP(Document doc, Database db, Transaction transaction)
        {
            PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;
            PromptSelectionResult sr = doc.Editor.SelectImplied();

                    if (sr.Status == PromptStatus.OK)
                    {
                        SelectionSet ss = sr.Value;
                        if (ss.Count<maxsimple)
                        {
                            ChangerSimple(tm, transaction, ss);
                        }
                        else
                        {
                            ChangerBig(tm, transaction, ss);
                        }
                    }
                    else
                    {
                        new CurLayerParser(db);
                    }
        }

        private static void ChangerSimple(PlatformDb.DatabaseServices.TransactionManager tm, Transaction myT, SelectionSet ss)
        {
            foreach (Entity ent in from ObjectId elem in ss.GetObjectIds()
                                                let ent = (Entity)tm.GetObject(elem, OpenMode.ForWrite)
                                                select ent)
            {
                try 
                {
                    EntityLayerParser entlp = new EntityLayerParser(ent, myT);
                }
                catch(WrongLayerException)
                {
                    continue;
                }
                
            }
        }

        private static void ChangerBig(PlatformDb.DatabaseServices.TransactionManager tm, Transaction myT, SelectionSet ss)
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
                        dct.Add(ent.Layer, new EntityLayerParser(ent, myT));
                    }
                    catch (WrongLayerException) 
                    {
                        continue;
                    }
                }
            }
        }
    }
    public static class LayerChecker
    {
        public static void Check(string layername)
        {
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
                            LayerProps lp;
                            SimpleLayerParser laypars = new SimpleLayerParser(layername);
                            bool success = LayerProperties.Dictionary.TryGetValue(laypars.TrueName, out lp);
                            if (!success) { throw new NoPropertiesException("Нет стандартов для слоя"); }
                            LinetypeTable ltt = (LinetypeTable)tm.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false);
                            if (!ltt.Has(lp.LTName))
                            {
                                db.LoadLineTypeFile(lp.LTName, @"C:\Users\ekono\source\repos\NC_EngTools\NC_EngTools\StandartFiles\СТАНДАРТ1.lin");
                            }
                            ObjectId lttrId = SymbolUtilityServices.GetLinetypeContinuousId(db);
                            foreach (ObjectId elem in ltt)
                            {
                                LinetypeTableRecord lttr = (LinetypeTableRecord)tm.GetObject(elem, OpenMode.ForRead);
                                if (lttr.Name.ToUpper()==lp.LTName.ToUpper()) { lttrId = lttr.Id; break; }
                                
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
                            transaction.Commit();
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch(NoPropertiesException)
                    {
                        throw new NoPropertiesException("Проверка слоя не удалась");
                    }
                    
                    catch (System.Exception ex)
                    {
                        throw new System.Exception(ex.Message);
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }

            }
        }
    }
}


    namespace TestCommands
{
    using HostMgd.ApplicationServices;
    using HostMgd.EditorInput;
    using Teigha.DatabaseServices;
    using Teigha.Runtime;
    using Platform = HostMgd;
    using PlatformDb = Teigha;
    public class TEST_12
        {
            [CommandMethod("TEST_COMMAND_2", CommandFlags.UsePickSet)]
            public void TestCommand2()
            {
                Database db = HostApplicationServices.WorkingDatabase;
                Document doc = Application.DocumentManager.MdiActiveDocument;
                PlatformDb.DatabaseServices.TransactionManager tm = db.TransactionManager;

                using (Transaction myT = tm.StartTransaction())
                {
                    try
                    {
                        PromptSelectionResult sr = doc.Editor.SelectImplied();
                        if (sr.Status == PromptStatus.OK)
                        {
                            SelectionSet ss = sr.Value;
                            foreach (ObjectId elem in ss.GetObjectIds())
                            {
                                Entity ent = (Entity)tm.GetObject(elem, OpenMode.ForWrite);
                                ent.Color = PlatformDb.Colors.Color.FromRgb(255, 0, 0);
                            }
                            myT.Commit();
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    finally { myT.Dispose(); }
                }
            }

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

                PromptStringOptions opts = new PromptStringOptions("Введите имя нового слоя");
                opts.AllowSpaces = true;
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
                                LayerTableRecord ltrec = new LayerTableRecord();
                                ltrec.Name = newLayerName;
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



