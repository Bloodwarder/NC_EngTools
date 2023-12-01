using Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;



namespace LayerWorks.ExternalData
{
    abstract internal class ExcelDictionaryDataProvider<TKey, TValue> : DictionaryDataProvider<TKey, TValue> //where TValue : struct
    {
        internal string Path { get; set; }
        private protected string sheetname;
        private readonly FileInfo _fileInfo;

        internal ExcelDictionaryDataProvider(string path, string sheetname)
        {
            Path = path;
            _fileInfo = new FileInfo(Path);
            if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            this.sheetname = sheetname;
        }

        public override Dictionary<TKey, TValue> GetDictionary()
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };

            Workbook xlwb = xlapp.Workbooks.Open(_fileInfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            Worksheet ws = xlwb.Worksheets[sheetname] as Worksheet;
            XmlSerializableDictionary<TKey, TValue> dct = new XmlSerializableDictionary<TKey, TValue>();
            try
            {
                Excel.Range rng = ((Excel.Range)ws.Cells[1, 1]).CurrentRegion;
                rng = rng.Offset[1, 0].Resize[rng.Rows.Count - 1, rng.Columns.Count];
                for (int i = 1; i < rng.Rows.Count + 1; i++)
                {
                    //TKey key = (TKey)rng.Cells[i, 1].Value;
                    try
                    {
                        TKey key = (TKey)Convert.ChangeType(((Excel.Range)rng.Cells[i, 1]).Value, typeof(TKey));
                        dct.Add(key, CellsExtract(((Excel.Range)rng.Cells[i, 2])));
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
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
            return dct;
        }

        public override void OverwriteSource(Dictionary<TKey, TValue> dictionary)
        {
            Application xlapp = new Application
            {
                DisplayAlerts = false
            };

            Workbook xlwb = xlapp.Workbooks.Open(_fileInfo.FullName, ReadOnly: true, IgnoreReadOnlyRecommended: true);
            try
            {
                CellsImport(xlwb, dictionary);
            }
            finally
            {
                xlwb.Close(SaveChanges: false);
                xlapp.Quit();
            }
        }


        abstract private protected TValue CellsExtract(Excel.Range rng);
        abstract private protected void CellsImport(Workbook xlwb, Dictionary<TKey, TValue> importeddictionary);
    }
}