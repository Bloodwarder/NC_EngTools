using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace LoaderCore.Utilities
{
    public class NcetFileCommandLogger : ILogger<NcetCommand>, IDisposable
    {
        readonly FileInfo _logFile;
        readonly LogLevel _logLevel;
        readonly StreamWriter _writer;
        public NcetFileCommandLogger(IConfiguration configuration)
        {
            FileInfo dwgFile = new(Workstation.Database.Filename);
            string dateTime = DateTime.Now.ToLongTimeString().Replace(":", "-");
            _logFile = new(Path.Combine(dwgFile.DirectoryName!, $"CommandLog_{dateTime}.txt"));

            _logLevel = configuration.GetValue<LogLevel>("Logging:CommandLogLevel");
            _writer = _logFile.CreateText();
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return this;
        }

        public void Dispose()
        {
            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
            GC.SuppressFinalize(this);
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _writer.WriteLine(formatter(state, exception));
        }

    }
}
