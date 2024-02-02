//System
using System.Linq;
//nanoCAD
using HostMgd.EditorInput;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using Teigha.Colors;

//internal modules
using LoaderCore.Utilities;
using LayerWorks.LayerProcessing;
using LayerWorks.Dictionaries;
using LayerWorks23.LayerProcessing;
using LayersIO.ExternalData;

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayersCommands
    {
        private const string ExtProjectAuxDataKey = "ExternalProject";
        internal static string PrevStatus = "Сущ";
        internal static string PrevExtProject = "";
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        [CommandMethod("КАЛЬКА")]
        public static void TransparentOverlayToggle()
        {
            Workstation.Define();
            Database db = Workstation.Database;
            TransactionManager tm = Workstation.TransactionManager;

            string tgtlayer = LayerWrapper.StandartPrefix + "_Калька";

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite, false) as LayerTable;
                if (!lt.Has(tgtlayer))
                {
                    System.Drawing.Color color = System.Drawing.Color.FromArgb(166, 255, 255, 255);
                    LayerTableRecord ltrec = new()
                    {
                        Name = tgtlayer,
                        Color = Color.FromColor(color),
                        //Color = Color.FromRgb(255, 255, 255),
                        //Transparency = new Transparency(166)
                    };
                    lt.Add(ltrec);
                    transaction.AddNewlyCreatedDBObject(ltrec, true);
                }
                else
                {
                    LayerTableRecord ltrec = (from ObjectId elem in lt
                                              let ltr = (LayerTableRecord)transaction.GetObject(elem, OpenMode.ForWrite, false)
                                              where ltr.Name == tgtlayer
                                              select ltr)
                                              .FirstOrDefault();
                    if (ltrec.IsFrozen || ltrec.IsOff)
                    {
                        ltrec.IsOff = false;
                        ltrec.IsFrozen = false;
                    }
                    else
                    {
                        ltrec.IsOff = true;
                    }

                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Изменение статуса объекта в соответствии с данными LayerParser
        /// </summary>
        [CommandMethod("ИЗМСТАТУС", CommandFlags.Redraw)]
        public static void LayerStatusChange()
        {
            Workstation.Define();

            PromptKeywordOptions pko = new($"Укажите статус объекта <{PrevStatus}> [Сущ/Демонтаж/Проект/Неутв/Неутв_демонтаж/Неутв_реорганизация]", "Сущ Демонтаж Проект Неутв Неутв_демонтаж Неутв_реорганизация")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult res = Workstation.Editor.GetKeywords(pko);
            if (res.Status == PromptStatus.OK) { PrevStatus = res.StringResult; }
            StatusTextDictionary.StTxtDictionary.TryGetValue(res.StringResult, out int val);
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info => info.SwitchStatus(val.ToString())));
                    //ActiveLayerWrappers.StatusSwitch((Status)val);
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                catch (System.Exception ex)
                {
                    Workstation.Editor.WriteMessage(ex.Message);
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public static void LayerAlter()
        {
            Workstation.Define();

            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                //string str = LayerAlteringDictionary.GetValue(MainName, out bool success);
                //if (!success) { return; }
                //MainName = str;

                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info =>
                    {
                        string name = LayerAlteringDictionary.GetValue(info.MainName, out bool success);
                        if (success)
                            info.AlterSecondaryClassifier(name);
                        return;
                    }));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    Workstation.Editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою приписки, обозначающей переустройство
        /// </summary>
        [CommandMethod("ПЕРЕУСТРОЙСТВО", CommandFlags.Redraw)]
        public static void LayerReconstruction()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerWrappers.List.ForEach(l => l.AlterLayerInfo(info => { info.SuffixTagged = !info.SuffixTagged; }));
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою имени внешнего проекта (неутверждаемого)
        /// </summary>
        [CommandMethod("ВНЕШПРОЕКТ", CommandFlags.Redraw)]
        public static void ExtAssign()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            PromptStringOptions pso = new($"Введите имя проекта, согласно которому отображён выбранный объект")
            {
                AllowSpaces = true,
                DefaultValue = PrevExtProject,
                UseDefaultValue = true,
            };
            PromptResult result = editor.GetString(pso);
            string extProjectName;
            if (result.Status != (PromptStatus.Error | PromptStatus.Cancel))
            {
                extProjectName = result.StringResult;
                PrevExtProject = extProjectName;
            }
            else
            {
                return;
            }

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerWrappers.List.ForEach(w => w.AlterLayerInfo(info => info.ChangeAuxilaryData(ExtProjectAuxDataKey, extProjectName)));
                    //ActiveLayerWrappers.ExtProjNameAssign(extProjectName);
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }
        }

        /// <summary>
        /// Приведение свойств объекта или текущих переменных чертежа к стандарту (ширина и масштаб типов линий)
        /// </summary>
        [CommandMethod("СВС", CommandFlags.Redraw)]
        public static void StandartLayerValues()
        {
            Workstation.Define();
            TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerWrappers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerWrappers.Flush();
                }
            }

        }

        /// <summary>
        /// Изменение обрабатываемого префикса слоёв
        /// </summary>
        [CommandMethod("ПРЕФИКС")]
        public void ChangePrefix()
        {
            Workstation.Define();
            List<string> additionalOptions = new()
            {
                "Переопределить",
                "Загрузить"
            };
            List<string> prefixes = NameParser.LoadedParsers.Keys.OrderBy(k => k).Concat(additionalOptions).ToList();

            PromptKeywordOptions pko = new($"Выберите префикс обрабатываемых слоёв <{LayerWrapper.StandartPrefix}> [{string.Join("/", prefixes)}", string.Join(" ", prefixes))
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult result = Workstation.Editor.GetKeywords(pko);
            if (result.Status != PromptStatus.OK)
                return;
            if (result.StringResult == "Переопределить")
            {
                RedefinePrefix();
                return;
            }
            if (result.StringResult == "Загрузить")
            {
                LoadNewParser();
                return;
            }
            string newprefix = result.StringResult;
            LayerWrapper.StandartPrefix = newprefix;
        }

        private void RedefinePrefix()
        {
            throw new NotImplementedException();
        }

        private void LoadNewParser()
        {
            throw new NotImplementedException();
        }
    }

}






