using HostMgd.ApplicationServices;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;

namespace LoaderCore.Utilities
{
    public class NcetEditorConsoleLogger : ILogger
    {
        private readonly LogLevel _logLevel;
        internal NcetEditorConsoleLogger()
        {
            _logLevel = LogLevel.Information;
        }

        public NcetEditorConsoleLogger(IConfiguration configuration)
        {
            _logLevel = configuration.GetValue<LogLevel>("Logging:EditorLogLevel");
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;


        public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                try
                {
                    string message = formatter(state, exception);
                    Workstation.Editor.WriteMessage(message);
                }
                catch (AccessViolationException ex)
                {
                    string str = ex.Message;
                }
            }
        }
    }
}
