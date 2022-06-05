

namespace Dictionaries
{
    using System.Collections.Generic;
    static class StatusTextDictionary
    {
        public static Dictionary<string, int> StTxtDictionary { get; private set; }
        static StatusTextDictionary()
        {
            StTxtDictionary = new Dictionary<string, int>();
            StTxtDictionary.Add("Сущ", 0);
            StTxtDictionary.Add("Демонтаж", 1);
            StTxtDictionary.Add("Проект", 2);
            StTxtDictionary.Add("Неутв", 3);
            StTxtDictionary.Add("Неутв_демонтаж", 4);
            StTxtDictionary.Add("Неутв_реорганизация", 5);
        }

    }
}