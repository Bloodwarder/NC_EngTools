using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static class ExcelStructIO<T> where T : struct
        {
            internal static T Read(Range rng)
            {
                if (typeof(T).IsPrimitive)
                    return (T)rng.Value;

                T strct = new T();
                object o = strct;

                FieldInfo[] fieldInfo = typeof(T).GetFields();
                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    // Не знаю, нужно ли приводить значение ячейки экселя к точному типу перед упаковкой в объект, разобраться позже, пока работает так
                    var cellvalue = rng.Offset[0, i].Value;
                    if (cellvalue != null)
                        fieldInfo[i].SetValue(o, (object)Convert.ChangeType(cellvalue, fieldInfo[i].FieldType));
                }
                return (T)o;
            }
            internal static void Write(T sourcestruct, Range targetrange, Dictionary<string, int> indexdictionary)
            {
                // Принимаем экземпляр структуры для записи, строку для записи (с числом ячеек равным числу полей структуры), словарь индексов заголовков в эксель таблице
                // Получаем поля структуры через рефлексию
                List<FieldInfo> list = typeof(T).GetFields().ToList();
                // Значение каждого поля записываем в ячейки эксель в соответствии с индексом по имени поля (имя поля должно совпадать с заголовком)
                foreach (FieldInfo fi in list)
                {
                    targetrange.Cells[1, indexdictionary[fi.Name]].Value = fi.GetValue(sourcestruct);
                }
            }
        }
    }
}