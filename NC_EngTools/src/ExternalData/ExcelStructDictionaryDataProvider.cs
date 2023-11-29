using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;

namespace LayerWorks.ExternalData
{
    internal class ExcelStructDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue> where TValue : struct
    {
        internal ExcelStructDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(Range rng)
        {
            return ExcelStructIO<TValue>.Read(rng);
        }

        private protected override void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Создаём словарь для индексирования заголовков, заполняем его
            Dictionary<string, int> labelindex = new Dictionary<string, int>();
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            for (int i = 1; i < rng.Columns.Count; i++)
            {
                labelindex.Add(rng[1, i].Value, i);
            }
            // Обрезаем заголовки
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
            // Заполняем строки через ExcelStructIO
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                rng.Cells[counter, 1].Value = keyValue.Key;
                ExcelStructIO<TValue>.Write(keyValue.Value, rng.Range[rng.Cells[counter, 2], rng.Cells[counter, rng.Columns.Count]], labelindex);
                counter++;
            }
        }
    }
}