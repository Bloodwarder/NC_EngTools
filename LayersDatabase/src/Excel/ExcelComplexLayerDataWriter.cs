using NPOI.SS.UserModel;
using System.Reflection;

namespace LayersIO.Excel
{
    public class ExcelComplexLayerDataWriter<TKey, TValue> : ExcelLayerDataWriter<TKey, TValue> where TKey : class where TValue : class, new()
    {
        public ExcelComplexLayerDataWriter(string path, string sheetname) : base(path, sheetname) { }

        private protected override void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Создаём словарь для индексирования заголовков, заполняем его
            Dictionary<string, int> labelindex = new();
            ISheet ws = xlwb.GetSheet(sheetname);

            for (int i = 1; i < ws.GetRow(1).Count(); i++)
            {
                labelindex.Add(ws.GetRow(1).Cells[i].StringCellValue, i);
            }
            // Заполняем строки через ExcelStructIO
            int counter = 2;
            foreach (KeyValuePair<TKey, TValue> keyValue in importeddictionary)
            {
                IRow row = ws.GetRow(counter);
                row.Cells[1].SetCellValue(keyValue.Key!.ToString());
                DataWrite(keyValue.Value!, row, labelindex);
                counter++;
            }
        }

        internal static void DataWrite<T>(T sourcestruct, IRow targetrow, Dictionary<string, int> indexdictionary) where T : class
        {
            // Принимаем экземпляр структуры для записи, строку для записи (с числом ячеек равным числу полей структуры), словарь индексов заголовков в эксель таблице
            // Получаем поля структуры через рефлексию
            List<FieldInfo> list = typeof(T).GetFields().ToList();
            // Значение каждого поля записываем в ячейки эксель в соответствии с индексом по имени поля (имя поля должно совпадать с заголовком)
            foreach (FieldInfo fi in list)
            {
                targetrow.Cells[indexdictionary[fi.Name]].SetCellValue(fi.GetValue(sourcestruct).ToString());
            }
        }

    }
}