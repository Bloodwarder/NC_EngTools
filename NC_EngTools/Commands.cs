//System
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
using System;
//internal modules
using ModelspaceDraw;
using Teigha.Geometry;
using System.Text.RegularExpressions;
using Teigha.Colors;

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
                    ActiveLayerParsers.StatusSwitch((Status)val);
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
                        new ChapterStoreLayerParser(ltr);
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
            Workstation.Define(out Document doc, out _, out Teigha.DatabaseServices.TransactionManager tm);
            using (Transaction myT = tm.StartTransaction())
            {
                ChapterStoreLayerParser rlp = new ChapterStoreLayerParser((LayerTableRecord)sender);
                rlp.Push(ActiveChapterState[doc]);
                myT.Commit();
            }

        }
    }

    public class TestClass1
    {

        [CommandMethod("ТЕСТ_123")]
        public void Test111()
        {
            SimpleTestObjectDraw draw = new SimpleTestObjectDraw(new PlatformDb.Geometry.Point2d(0d, 0d));
            draw.Draw();
        }
    }

    public class Labeler
    {
        [CommandMethod("ПОДПИСЬ")]
        public void LabelDraw()
        {
            using (Transaction tr = Workstation.TransactionManager.StartTransaction())
            {
                //выбираем полилинию
                PromptEntityOptions peo = new PromptEntityOptions("Выберите полилинию")
                { };
                peo.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult result = Workstation.Editor.GetEntity(peo);
                if (result.Status != PromptStatus.OK)
                {
                    Workstation.Editor.WriteMessage("Ошибка выбора");
                    return;
                }

                //выбираем точку вставки подписи и находим ближайшую точку на полилинии
                PromptPointOptions ppo = new PromptPointOptions("Укажите точку на сегменте полилинии")
                { };
                PromptPointResult pointresult = Workstation.Editor.GetPoint(ppo);
                if (result.Status != PromptStatus.OK)
                    return;
                Point3d point = pointresult.Value;
                Polyline polyline = (Polyline)Workstation.TransactionManager.GetObject(result.ObjectId, OpenMode.ForRead); ;
                Point3d pointonline = polyline.GetClosestPointTo(point, false);

                //проверяем, какому сегменту принадлежит точка и вычисляем его направление
                LineSegment2d ls = new LineSegment2d();
                for (int i = 0; i<polyline.NumberOfVertices-1; i++)
                {
                    ls = polyline.GetLineSegment2dAt(i);
                    Line2d l2d = ls.GetLine();
                    if (segmentCheck(pointonline.Convert2d(new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1))), ls))
                        break;
                }
                Vector2d v2d = ls.Direction;

                //вводим текст для подписи
                PromptResult pr = Workstation.Editor.GetString("Введите текст подписи (д или d в начале строки - знак диаметра");
                string text;
                if (pr.Status != (PromptStatus.Error | PromptStatus.Cancel))
                {
                    text = pr.StringResult=="" ? "%%C000" : pr.StringResult;
                }
                else
                {
                    text = "%%C000"; //по умолчанию
                }

                //ищем таблицу блоков и моделспейс
                BlockTable blocktable = tr.GetObject(Workstation.Database.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
                BlockTableRecord modelspace = tr.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                //создаём текст
                MText mtext = new MText();
                mtext.BackgroundFill = true;
                mtext.UseBackgroundColor = true;
                mtext.BackgroundScaleFactor = 1.1d;
                TextStyleTable txtstyletable = tr.GetObject(Workstation.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                mtext.TextStyleId =txtstyletable["Standard"];
                mtext.Contents = Regex.Replace(text, "^(д|d)", "%%C"); //заменяем первую букву д на знак диаметра
                mtext.TextHeight = 3.6d;
                mtext.SetAttachmentMovingLocation(AttachmentPoint.MiddleCenter);
                mtext.Location = point;
                mtext.Color = polyline.Color;
                mtext.Layer = polyline.Layer; //устанавливаем цвет и слой как у полилинии
                mtext.Rotation = (v2d.Angle * 180/Math.PI > 270 ||  v2d.Angle * 180/Math.PI < 90) ? v2d.Angle : v2d.Angle + Math.PI; //вычисляем угол поворота с учётом читаемости
                //добавляем в модель и в транзакцию
                modelspace.AppendEntity(mtext);
                tr.AddNewlyCreatedDBObject(mtext, true); // и в транзакцию

                tr.Commit();
            }
        }

        private bool segmentCheck(Point2d point, LineSegment2d entity)
        {
            Point2d p1 = entity.StartPoint;
            Point2d p2 = entity.EndPoint;

            double maxx = new double[] { p1.X, p2.X }.Max();
            double maxy = new double[] { p1.Y, p2.Y }.Max();
            double minx = new double[] { p1.X, p2.X }.Min();
            double miny = new double[] { p1.Y, p2.Y }.Min();

            if (maxx-minx<5)
            {
                maxx = maxx+5;
                minx = minx-5;
            }
            if (maxy-miny<5)
            {
                maxy = maxy+5;
                miny = miny-5;
            }
            //bool b1 = (point.X>minx & point.X<maxx) & (point.Y>miny & point.Y<maxy);
            return (point.X>minx & point.X<maxx) & (point.Y>miny & point.Y<maxy);
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
                new CurrentLayerParser();
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
                        LayerProps lp = LayerPropertiesDictionary.GetValue(layername, out bool propsgetsuccess);
                        LinetypeTable ltt = (LinetypeTable)tm.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false);
                        CheckLinetype(lp.LTName, out bool ltgetsuccess);
                        ObjectId lttrId = SymbolUtilityServices.GetLinetypeContinuousId(db);
                        if (ltgetsuccess)
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
                        LayerAdded?.Invoke(ltrec, e);

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
        internal static void CheckLinetype(string linetypename, out bool ltgetsuccess)
        {
            PlatformDb.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Database db = Workstation.Database;
            LinetypeTable ltt = (LinetypeTable)tm.GetObject(db.LinetypeTableId, OpenMode.ForWrite, false);
            ltgetsuccess = true;
            if (!ltt.Has(linetypename))
            {
                FileInfo fi = new FileInfo(PathOrganizer.GetPath("Linetypes"));
                try
                {
                    db.LoadLineTypeFile(linetypename, fi.FullName);
                    return;
                }
                catch
                { ltgetsuccess = false; }
            }
        }
    }

    static class Workstation
    {

        public static Document Document => Application.DocumentManager.MdiActiveDocument;
        public static Database Database => HostApplicationServices.WorkingDatabase;
        public static PlatformDb.DatabaseServices.TransactionManager TransactionManager => Database.TransactionManager;
        public static Editor Editor => Document.Editor;


        internal static void Define(out Document doc)
        {
            doc = Document;
        }
        internal static void Define(out Database db)
        {
            db = Database;
        }
        internal static void Define(out Document doc, out Database db)
        {
            Define(out doc);
            Define(out db);
        }
        internal static void Define(out Document doc, out Database db, out PlatformDb.DatabaseServices.TransactionManager tm)
        {
            Define(out doc, out db);
            tm = TransactionManager;
        }
        internal static void Define(out PlatformDb.DatabaseServices.TransactionManager tm)
        {
            tm = TransactionManager;
        }
        internal static void Define(out Document doc, out Database db, out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed)
        {
            Define(out doc, out db, out tm);
            ed = Editor;
        }
        internal static void Define(out PlatformDb.DatabaseServices.TransactionManager tm, out Editor ed)
        {
            tm = TransactionManager;
            ed = Editor;
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
}




