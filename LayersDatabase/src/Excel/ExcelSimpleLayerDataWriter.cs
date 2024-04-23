using NPOI.SS.UserModel;

namespace LayersIO.Excel
{
    public class ExcelSimpleLayerDataWriter<TKey, TValue> : ExcelLayerDataWriter<TKey, TValue> where TKey : class where TValue : class
    {
        public ExcelSimpleLayerDataWriter(string path, string sheetname) : base(path, sheetname)
        {
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
            for (int i = counter; i < ws.LastRowNum; i++)
            {
                ws.GetRow(i).Cells.ForEach(c => c.SetCellValue(string.Empty));
            }
        }
    }

}