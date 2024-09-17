using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetSimpleLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;


        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = string.Empty;
            message = formatter(state, exception);
            LoggingRouter.WriteLog?.Invoke(message);
        }

    }
}
