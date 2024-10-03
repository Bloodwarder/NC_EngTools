﻿using HostMgd.EditorInput;
using NanocadUtilities;
using System.Data;
using System.Text.RegularExpressions;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace Utilities
{
    /// <summary>
    /// Класс команд для работы с текстом (мтекстом)
    /// </summary>
    public static class TextProcessor
    {
        /// <summary>
        /// Убирает форматирование из МТекста, если оно задано не стилем, а внутри текста
        /// </summary>
        [CommandMethod("СМТ", CommandFlags.UsePickSet)]
        public static void StripMText()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new PromptSelectionOptions()
                { };
                PromptSelectionResult result = Workstation.Editor.GetSelection(pso);
                if (result.Status != PromptStatus.OK)
                    return;
                var texts = from ObjectId id in result.Value.GetObjectIds()
                            let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                            where entity is MText
                            select entity as MText;
                // если текст заключен в шаблон для форматирования ({/ ... ; ТЕКСТ }), оставить только чистый текст (ТЕКСТ)
                Regex regex = new Regex(@"(?<=(^{(\\.*;)+\b)).+(?=(\b}$))");
                foreach (var text in texts)
                {
                    var matches = regex.Matches(text.Contents);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            match.Result(match.Value);
                        }
                    }
                }
                transaction.Commit();
                // Пока работает только на целиком отформатированный текст
            }
        }

        /// <summary>
        /// Назначает тексту нулевую ширину для удобства автоматической обработки выравнивания и фона
        /// </summary>
        [CommandMethod("НУЛЕВАЯШИРИНАТЕКСТА")]
        public static void AssignZeroWidth()
        {
            Workstation.Define();
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new PromptSelectionOptions()
                { };
                PromptSelectionResult result = Workstation.Editor.GetSelection(pso);
                if (result.Status != PromptStatus.OK)
                    return;
                var texts = from ObjectId id in result.Value.GetObjectIds()
                            let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                            where entity is MText
                            select entity as MText;
                foreach (var text in texts)
                {
                    text.Width = 0d;
                }
                transaction.Commit();
            }
        }
    }

}
