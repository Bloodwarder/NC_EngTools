using LayersIO.ExternalData;
using LayersIO.Xml;
using NPOI.SS.UserModel;
using System.Diagnostics;
using System.IO;



namespace LayersIO.Excel
{
    abstract public class ExcelLayerDataProvider<TKey, TValue> : LayerDataProvider<TKey, TValue> where TKey : class//where TValue : struct
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

        public override Dictionary<TKey, TValue> GetData()
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

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {


            IWorkbook xlwb = WorkbookFactory.Create(_fileInfo.FullName, null, false);
            try
            {
                CellsImport(xlwb, dictionary);
            }
            finally
            {
                xlwb.Close();
            }
        }


        abstract private protected TValue CellsExtract(ICell rng);
        abstract private protected void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary);
    }
}