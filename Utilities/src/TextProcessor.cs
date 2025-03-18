using HostMgd.EditorInput;
using LoaderCore.NanocadUtilities;
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
        public static void StripMText()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new()
                { };
                PromptSelectionResult result = Workstation.Editor.GetSelection(pso);
                if (result.Status != PromptStatus.OK)
                    return;
                var texts = from ObjectId id in result.Value.GetObjectIds()
                            let entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity
                            where entity is MText
                            select entity as MText;
                // если текст заключен в шаблон для форматирования ({/ ... ; ТЕКСТ }), оставить только чистый текст (ТЕКСТ)
                Regex regex = new(@"(?<=(^{(\\.*;)+\b)).+(?=(\b}$))");
                foreach (var text in texts)
                {
                    var matches = regex.Matches(text.Contents).Cast<Match>();
                    foreach (Match match in matches)
                    {
                        match.Result(match.Value);
                    }
                }
                transaction.Commit();
                // Пока работает только на целиком отформатированный текст
            }
        }

        /// <summary>
        /// Назначает тексту нулевую ширину для удобства автоматической обработки выравнивания и фона
        /// </summary>
        public static void AssignZeroWidth()
        {
            using (Transaction transaction = Workstation.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions pso = new()
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
                    text.Height = 0d;
                }
                transaction.Commit();
            }
        }
    }

}
