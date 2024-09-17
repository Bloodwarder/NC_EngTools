namespace LoaderCore.Utilities
{
    public static class LoggingRouter
    {
        public static Log? WriteLog { get; set; }

        public delegate void Log(string message);

    }
}
