namespace LoaderCore.Interfaces
{
    public interface IReportWriter<T> where T : class
    {
        public void PrepareReport(T[] data);
        public void ShowReport();
    }
}
