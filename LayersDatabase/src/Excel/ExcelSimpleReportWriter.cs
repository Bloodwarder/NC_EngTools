using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics;

namespace LayersIO.Excel
{
    public class ExcelSimpleReportWriter<T> where T : class
    {
        internal string Path { get; set; }
        private string _sheetName;
        private readonly FileInfo _fileInfo;

        private static Dictionary<Type, CellType> _cellTypes { get; } = new()
        {
            [typeof(string)] = CellType.String,
            [typeof(bool)] = CellType.Boolean,
            [typeof(double)] = CellType.Numeric,
            [typeof(int)] = CellType.Numeric,
            [typeof(byte)] = CellType.Numeric,
            [typeof(uint)] = CellType.Numeric,
            [typeof(DateTime)] = CellType.Unknown,
        };

        public ExcelSimpleReportWriter(string path, string sheetname)
        {
            Path = path;
            _fileInfo = new FileInfo(Path);
            //if (!_fileInfo.Exists) { throw new System.Exception("Файл не существует"); }
            _sheetName = sheetname;
        }

        public void ExportToExcel(T[] data)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet(_sheetName);

            // Create header row
            IRow headerRow = sheet.CreateRow(0);
            var properties = typeof(T).GetProperties();
            // Create cell styles for formatting
            ICellStyle headerStyle = workbook.CreateCellStyle();
            IFont headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerFont.FontName = "TimesNewRoman";
            headerFont.FontHeightInPoints = 12;
            headerStyle.SetFont(headerFont);
            headerStyle.WrapText = true;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            headerStyle.Alignment = HorizontalAlignment.Center;
            SetBorderStyle(headerStyle, BorderStyle.Thin);

            IFont dataFont = workbook.CreateFont();
            dataFont.CloneStyleFrom(headerFont);
            dataFont.IsBold = false;

            ICellStyle dataStyle = workbook.CreateCellStyle();
            SetBorderStyle(dataStyle, BorderStyle.Thin);
            dataStyle.VerticalAlignment = VerticalAlignment.Center;
            dataStyle.Alignment = HorizontalAlignment.Center;
            dataStyle.WrapText = true;
            dataStyle.SetFont(dataFont);

            for (int i = 0; i < properties.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(properties[i].Name);
                cell.CellStyle = headerStyle;
                if (properties[i].Name.Contains("name", StringComparison.InvariantCultureIgnoreCase))
                    sheet.SetColumnWidth(i, 256 * 50);
            }

            // Populate data rows
            for (int i = 0; i < data.Length; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                for (int j = 0; j < properties.Length; j++)
                {
                    ICell cell = dataRow.CreateCell(j, _cellTypes[properties[j].PropertyType]);
                    dynamic value = properties[j].GetValue(data[i]);
                    cell.SetCellValue(value);
                    cell.CellStyle = dataStyle;
                }
            }

            // Write the workbook to a file
            using (FileStream fileStream = new FileStream(_fileInfo.FullName, FileMode.CreateNew, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }

            OpenExcelFile(_fileInfo.FullName);
        }

        private static void OpenExcelFile(string filePath)
        {
            //Process[] processes = Process.GetProcessesByName("excel");

            //if (processes.Length > 0)
            //{
            //    foreach (Process process in processes)
            //    {
            //        if (!process.HasExited)
            //        {
            //            process.StartInfo.FileName = "excel.exe";
            //            process.StartInfo.Arguments = "\"" + filePath + "\"";
            //            process.Start();
            //            return;
            //        }
            //    }
            //}

            ProcessStartInfo startInfo = new()
            {
                FileName = "excel.exe",
                Arguments = "\"" + filePath + "\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private static void SetBorderStyle(ICellStyle style, BorderStyle borderStyle)
        {
            style.BorderBottom = borderStyle;
            style.BorderLeft = borderStyle;
            style.BorderRight = borderStyle;
            style.BorderTop = borderStyle;
        }
    }
}
