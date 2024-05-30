using LayersIO.Xml;
using LoaderCore.Interfaces;
using NPOI.SS.UserModel;
using System.Diagnostics;



namespace LayersIO.Excel
{
    public abstract class ExcelLayerDataProvider<TKey, TValue> : ILayerDataProvider<TKey, TValue> where TKey : class//where TValue : struct
    {
        internal string Path { get; set; }
        private protected string sheetname;
        private readonly FileInfo _fileInfo;

        protected static Dictionary<Type, Func<ICell, object>> _valueHandler { get; } = new()
        {
            [typeof(string)] = c => c.StringCellValue,
            [typeof(bool)] = c => c.BooleanCellValue,
            [typeof(double)] = c => c.NumericCellValue,
            [typeof(int)] = c => c.NumericCellValue,
            [typeof(byte)] = c => c.NumericCellValue,
            [typeof(uint)] = c => c.NumericCellValue,
            [typeof(DateTime)] = c => c.DateCellValue,
        };

        internal ExcelLayerDataProvider(string path, string sheetname)
        {
            Path = path;
            _fileInfo = new FileInfo(Path);
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            this.sheetname = sheetname;
        }

        public Dictionary<TKey, TValue> GetData()
        {

            IWorkbook xlwb = WorkbookFactory.Create(_fileInfo.FullName, null, true);
            ISheet ws = xlwb.GetSheet(sheetname);
            XmlSerializableDictionary<TKey, TValue> dct = new XmlSerializableDictionary<TKey, TValue>();
            try
            {
                for (int i = 1; i < ws.LastRowNum + 1; i++)
                {
                    try
                    {
                        TKey key = (TKey)_valueHandler[typeof(TKey)](ws.GetRow(i).Cells[1]);
                        dct.Add(key, CellsExtract(ws.GetRow(i).Cells[2]));
                    }
                    catch
                    {
                        Debug.Print($"Data extraction error in row {i}");
                        continue;
                    }
                }
            }
            finally
            {
                xlwb.Close();
            }
            return dct;
        }

        abstract private protected TValue CellsExtract(ICell rng);

        public TValue? GetItem(TKey key)
        {
            IWorkbook xlwb = WorkbookFactory.Create(_fileInfo.FullName, null, true);
            ISheet ws = xlwb.GetSheet(sheetname);
            TValue? value = default;
            try
            {
                for (int i = 1; i < ws.LastRowNum + 1; i++)
                {
                    try
                    {
                        TKey comparedKey = (TKey)_valueHandler[typeof(TKey)](ws.GetRow(i).Cells[1]);
                        if (comparedKey == key)
                        {
                            value = CellsExtract(ws.GetRow(i).Cells[2]);
                            break;
                        }
                    }
                    catch
                    {
                        Debug.Print($"Key extraction error in row {i}");
                        continue;
                    }
                }
            }
            finally
            {
                xlwb.Close();
            }
            return value;
        }
        //abstract private protected void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary);
    }
}