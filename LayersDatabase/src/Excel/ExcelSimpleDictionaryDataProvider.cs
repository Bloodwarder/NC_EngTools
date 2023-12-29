using System.Collections.Generic;
using Npoi.Mapper;
using NPOI.SS;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace LayersIO.Excel
{
    public class ExcelSimpleDictionaryDataProvider<TKey, TValue> : ExcelDictionaryDataProvider<TKey, TValue> where TKey : notnull
    {
        public ExcelSimpleDictionaryDataProvider(string path, string sheetname) : base(path, sheetname) 
        {
        }
        private protected override TValue CellsExtract(ICell rng)
        {
            return (TValue)_valueHandler[typeof(TValue)](rng);
        }

        private protected override void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Обрезаем заголовки
            ISheet ws = xlwb.GetSheet(sheetname);
            // Просто записываем пары ключ-значения в 1 и 2 ячейки строки
            int counter = 1;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                IRow row = ws.GetRow(counter);
                row.Cells[1].SetCellValue(keyValue.Key!.ToString());
                row.Cells[2].SetCellValue(keyValue.Value!.ToString());
                counter++;
            }
            for(int i = counter; i < ws.LastRowNum; i++)
            {
                ws.GetRow(i).Cells.ForEach(c => c.SetCellValue(string.Empty));
            }
        }
    }
}