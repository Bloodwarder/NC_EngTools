using LoaderCore.Interfaces;

namespace LayersIO.Excel
{
    public class ExcelSimpleReportWriterFactory<T> : IReportWriterFactory<T> where T : class
    {
        public IReportWriter<T> CreateWriter(string path, params string[] args)
        {
            return new ExcelSimpleReportWriter<T>(path, args[0]);
        }
    }
}
