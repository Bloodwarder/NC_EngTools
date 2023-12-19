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

namespace LayerWorks.Commands
{
    /// <summary>
    /// Класс с командами для работы с классифицированными слоями
    /// </summary>
    public class LayersCommands
    {
        internal static string PrevStatus = "Сущ";
        internal static string PrevExtProject = "";
        /// <summary>
        /// Переключение кальки, при необходимости добавление её в чертёж
        /// </summary>
        [CommandMethod("КАЛЬКА")]
        public void TransparentOverlayToggle()
        {
            Workstation.Define();
            Database db = Workstation.Database;
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;

            string tgtlayer = LayerParser.StandartPrefix + "_Калька";

            using (Transaction transaction = tm.StartTransaction())
            {
                LayerTable lt = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite, false) as LayerTable;
                if (!lt.Has(tgtlayer))
                {

                    LayerTableRecord ltrec = new LayerTableRecord
                    {
                        Name = tgtlayer,
                        Color = Color.FromRgb(255, 255, 255),
                        Transparency = new Transparency(166)
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
        public void LayerStatusChange()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;


            PromptKeywordOptions pko = new PromptKeywordOptions($"Укажите статус объекта <{PrevStatus}> [Сущ/Демонтаж/Проект/Неутв/Неутв_демонтаж/Неутв_реорганизация]", "Сущ Демонтаж Проект Неутв Неутв_демонтаж Неутв_реорганизация")
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult res = ed.GetKeywords(pko);
            if (res.Status == PromptStatus.OK) { PrevStatus = res.StringResult; }
            StatusTextDictionary.StTxtDictionary.TryGetValue(res.StringResult, out int val);
            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.StatusSwitch((Status)val);
                    ActiveLayerParsers.Push();
                    transaction.Commit();
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
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Изменение типа объекта на альтернативный в соответствии с таблицей
        /// </summary>
        [CommandMethod("АЛЬТЕРНАТИВНЫЙ", CommandFlags.Redraw)]
        public void LayerAlter()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Alter();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою приписки, обозначающей переустройство
        /// </summary>
        [CommandMethod("ПЕРЕУСТРОЙСТВО", CommandFlags.Redraw)]
        public void LayerReconstruction()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor ed = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.ReconstrSwitch();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    ed.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Назначение объекту/слою имени внешнего проекта (неутверждаемого)
        /// </summary>
        [CommandMethod("ВНЕШПРОЕКТ", CommandFlags.Redraw)]
        public void ExtAssign()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            PromptStringOptions pso = new PromptStringOptions($"Введите имя проекта, согласно которому отображён выбранный объект")
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
                    ActiveLayerParsers.ExtProjNameAssign(extProjectName);
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
                }
            }
        }

        /// <summary>
        /// Приведение свойств объекта или текущих переменных чертежа к стандарту (ширина и масштаб типов линий)
        /// </summary>
        [CommandMethod("СВС", CommandFlags.Redraw)]
        public void StandartLayerValues()
        {
            Workstation.Define();
            Teigha.DatabaseServices.TransactionManager tm = Workstation.TransactionManager;
            Editor editor = Workstation.Editor;

            using (Transaction transaction = tm.StartTransaction())
            {
                try
                {
                    LayerChanger.UpdateActiveLayerParsers();
                    ActiveLayerParsers.Push();
                    transaction.Commit();
                }
                catch (WrongLayerException ex)
                {
                    editor.WriteMessage($"Текущий слой не принадлежит к списку обрабатываемых слоёв ({ex.Message})");
                }
                finally
                {
                    ActiveLayerParsers.Flush();
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
            Editor ed = Workstation.Editor;
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

}






