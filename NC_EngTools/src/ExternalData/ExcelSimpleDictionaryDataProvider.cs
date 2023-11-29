using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;

namespace LayerWorks.ExternalData
{
    internal class ExcelSimpleDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue>
    {
        internal ExcelSimpleDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(Range rng)
        {
            return (TValue)rng.Value;
        }

        private protected override void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Обрезаем заголовки
            Range rng = xlwb.Worksheets[sheetname].Cells[1, 1].CurrentRegion;
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
            // Просто записываем пары ключ-значения в 1 и 2 ячейки строки
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                rng.Cells[counter, 1].Value = keyValue.Key;
                rng.Cells[counter, 2].Value = keyValue.Value;
                counter++;
            }
        }
    }
}