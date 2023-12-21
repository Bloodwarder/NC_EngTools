using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;

namespace LayerWorks.ExternalData
{
    internal class ExcelSimpleDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue>
    {
        internal ExcelSimpleDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(Excel.Range rng)
        {
            return (TValue)rng.Value;
        }

        private protected override void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Обрезаем заголовки
            Worksheet ws = xlwb.Worksheets[sheetname] as Worksheet;
            Excel.Range rng = ((Excel.Range)ws.Cells[1, 1]).CurrentRegion;
            rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
            // Просто записываем пары ключ-значения в 1 и 2 ячейки строки
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                ((Excel.Range)rng.Cells[counter, 1]).Value = keyValue.Key;
                ((Excel.Range)rng.Cells[counter, 2]).Value = keyValue.Value;
                counter++;
            }
        }
    }
}