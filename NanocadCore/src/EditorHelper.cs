using HostMgd.EditorInput;


namespace NanocadUtilities
{
    public class EditorHelper
    {
        public static string GetStringKeywordResult(string[] keywordsArray, string message) => GetStringKeywordResult(keywordsArray, keywordsArray, message);

        public static string GetStringKeywordResult(string[] keywordsArray, string[] descriptionArray, string message)
        {
            PromptKeywordOptions pko = new($"{message} [{string.Join("/", keywordsArray)}]", string.Join(" ", descriptionArray))
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
    }
}
