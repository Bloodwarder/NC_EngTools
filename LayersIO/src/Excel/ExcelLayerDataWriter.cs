using LoaderCore.Interfaces;
using NPOI.SS.UserModel;



namespace LayersIO.Excel
{
    abstract public class ExcelLayerDataWriter<TKey, TValue> : ILayerDataWriter<TKey, TValue> where TKey : class//where TValue : struct
    {
        internal string Path { get; set; }
        private protected string sheetname;
        private readonly FileInfo _fileInfo;

        protected static Dictionary<Type, Func<ICell, object>> _valueHandler = new()
        {
            [typeof(string)] = c => c.StringCellValue,
            [typeof(bool)] = c => c.BooleanCellValue,
            [typeof(double)] = c => c.NumericCellValue,
            [typeof(int)] = c => c.NumericCellValue,
            [typeof(byte)] = c => c.NumericCellValue,
            [typeof(uint)] = c => c.NumericCellValue,
            [typeof(DateTime)] = c => c.DateCellValue,
        };

        internal ExcelLayerDataWriter(string path, string sheetname)
        {
            Path = path;
            _fileInfo = new FileInfo(Path);
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            this.sheetname = sheetname;
        }


        public void OverwriteSource(Dictionary<TKey, TValue> dictionary)
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
        abstract private protected void CellsImport(IWorkbook xlwb, Dictionary<TKey, TValue> importeddictionary);

        public void OverwriteItem(TKey key, TValue item)
        {
            throw new NotImplementedException();
        }
    }
}