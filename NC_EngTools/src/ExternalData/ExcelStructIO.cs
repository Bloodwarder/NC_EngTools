using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using System.Linq;
using System.Reflection;
using System;

namespace LayerWorks.ExternalData
{
    internal static class ExcelStructIO<T> where T : struct
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