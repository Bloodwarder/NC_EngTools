using System.Reflection;
using Npoi.Mapper;
using NPOI.SS.UserModel;

namespace LayersIO.Excel
{
    public class ExcelStructLayerDataProvider<TKey, TValue> : ExcelLayerDataProvider<TKey, TValue> where TValue : struct
    {
        public ExcelStructLayerDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(ICell rng)
        {
            return StructRead<TValue>(rng);
        }

        private protected override void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary)
        {
            // Создаём словарь для индексирования заголовков, заполняем его
            Dictionary<string, int> labelindex = new Dictionary<string, int>();
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
                StructWrite(keyValue.Value!, row, labelindex);
                counter++;
            }
        }


        internal static T StructRead<T>(ICell rng) where T : struct
        {
            if (typeof(T).IsPrimitive)
                return (T)_valueHandler[typeof(T)](rng);

            T strct = new T();
            object o = strct;

            FieldInfo[] fieldInfo = typeof(T).GetFields();
            IRow row = rng.Row;
            for (int i = 0; i < fieldInfo.Length; i++)
            {
                fieldInfo[i].SetValue(o, _valueHandler[fieldInfo[i].FieldType](row.Cells[i + 2]));
            }
            return (T)o;
        }
        internal static void StructWrite<T>(T sourcestruct, IRow targetrow, Dictionary<string, int> indexdictionary) where T : struct
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