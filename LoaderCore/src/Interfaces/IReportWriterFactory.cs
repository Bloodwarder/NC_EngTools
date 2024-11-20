namespace LoaderCore.Interfaces
{
    public interface IReportWriterFactory<T> where T : class
    {
        public IReportWriter<T> CreateWriter(string path, params string[] args);
    }
}
