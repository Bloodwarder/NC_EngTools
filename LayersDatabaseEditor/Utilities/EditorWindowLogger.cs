using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LayersDatabaseEditor.Utilities
{
    public class EditorWindowLogger : ILogger
    {
        private readonly LogLevel _logLevel;
        internal EditorWindowLogger()
        {
            _logLevel = LogLevel.Information;
        }

        public EditorWindowLogger(IConfiguration configuration)
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
                    // TODO: собственно, логгировать
                    //Workstation.Editor.WriteMessage(message);
                }
                catch (AccessViolationException ex)
                {
                    string str = ex.Message;
                }
            }
        }
    }
}
