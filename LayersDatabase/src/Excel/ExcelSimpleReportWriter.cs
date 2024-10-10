using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            IFont dataFont = workbook.CreateFont();
            dataFont.CloneStyleFrom(headerFont);
            dataFont.IsBold = false;

            ICellStyle dataStyle = workbook.CreateCellStyle();
            dataStyle.BorderBottom = BorderStyle.Thin;
            dataStyle.BorderLeft = BorderStyle.Thin;
            dataStyle.BorderRight = BorderStyle.Thin;
            dataStyle.BorderTop = BorderStyle.Thin;
            dataStyle.Alignment = HorizontalAlignment.Center;
            dataStyle.SetFont(dataFont);

            for (int i = 0; i < properties.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(properties[i].Name);
                cell.CellStyle = headerStyle;
            }

            // Populate data rows
            for (int i = 0; i < data.Length; i++)
            {
                IRow dataRow = sheet.CreateRow(i + 1);
                for (int j = 0; j < properties.Length; j++)
                {
                    ICell cell = dataRow.CreateCell(j, _cellTypes[properties[j].PropertyType]);
                    cell.SetCellValue(properties[j].GetValue(data[i])?.ToString());
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
            Process[] processes = Process.GetProcessesByName("excel");

            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    if (!process.HasExited)
                    {
                        process.StartInfo.FileName = "excel.exe";
                        process.StartInfo.Arguments = "\"" + filePath + "\"";
                        process.Start();
                        return;
                    }
                }
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = "excel.exe",
                Arguments = "\"" + filePath + "\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
}
