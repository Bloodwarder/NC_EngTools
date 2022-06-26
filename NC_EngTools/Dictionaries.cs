

namespace Dictionaries
{
    using System.Collections.Generic;
    static class StatusTextDictionary
    {
        internal static Dictionary<string, int> StTxtDictionary { get; private set; }
        static StatusTextDictionary()
        {
            StTxtDictionary = new Dictionary<string, int>
            {
                { "Сущ", 0 },
                { "Демонтаж", 1 },
                { "Проект", 2 },
                { "Неутв", 3 },
                { "Неутв_демонтаж", 4 },
                { "Неутв_реорганизация", 5 }
            };
        }

    }
}