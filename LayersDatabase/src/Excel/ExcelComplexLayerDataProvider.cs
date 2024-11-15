using NPOI.SS.UserModel;
using System.Reflection;

namespace LayersIO.Excel
{
    public class ExcelComplexLayerDataProvider<TKey, TValue> : ExcelLayerDataProvider<TKey, TValue> where TKey : class where TValue : class, new()
    {
        public ExcelComplexLayerDataProvider(string path, string sheetname) : base(path, sheetname) { }
        private protected override TValue CellsExtract(ICell rng)
        {
            return DataRead<TValue>(rng);
        }
        internal static T DataRead<T>(ICell rng) where T : class, new()
        {
            if (typeof(T).IsPrimitive)
                return (T)_valueHandler[typeof(T)](rng);

            T strct = new();
            object o = strct;

            FieldInfo[] fieldInfo = typeof(T).GetFields();
            IRow row = rng.Row;
            for (int i = 0; i < fieldInfo.Length; i++)
            {
                fieldInfo[i].SetValue(o, _valueHandler[fieldInfo[i].FieldType](row.Cells[i + 2]));
            }
            return (T)o;
        }
    }
}