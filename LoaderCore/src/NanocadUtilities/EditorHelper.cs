using HostMgd.EditorInput;
using System.Linq;

namespace LoaderCore.NanocadUtilities
{
    public class EditorHelper
    {
        public static string GetStringKeyword(string[] keywordsArray, string message) => GetStringKeyword(keywordsArray, keywordsArray, message);

        public static string GetStringKeyword(string[] keywordsArray, string[] descriptionArray, string message)
        {
            // Длины массивов ключей и описаний должны соответствовать
            if (keywordsArray.Length != descriptionArray.Length)
                throw new System.Exception("Длина исходных массивов отличается");
            // Если всего один элемент, то и выбирать нечего
            if (keywordsArray.Length == 1)
                return keywordsArray[0];
            PromptKeywordOptions pko = new($"{message} [{string.Join("/", descriptionArray)}]", string.Join(" ", keywordsArray))
            {
                AppendKeywordsToMessage = true,
                AllowNone = false,
                AllowArbitraryInput = false
            };
            PromptResult keywordResult = Workstation.Editor.GetKeywords(pko);
            if (keywordResult.Status != PromptStatus.OK)
                throw new System.Exception("Ошибка ввода");
            return keywordResult.StringResult;
        }

        public static string GetStringKeyword(string[] keywordsArray, string[] descriptionArray, string message, params string[] additional)
        {
            var newKeywords = keywordsArray.Concat(additional).ToArray();
            var newDescriptions = descriptionArray.Concat(additional).ToArray();
            return GetStringKeyword(newKeywords, newDescriptions, message);
        }
        public static string GetStringKeyword(string[] keywordsArray, string message, params string[] additional)
        {
            var newKeywords = keywordsArray.Concat(additional).ToArray();
            return GetStringKeyword(newKeywords, message);
        }
    }
}
