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
            string message = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Trace:
                    LoggingRouter.LogTrace?.Invoke(message);
                    goto case LogLevel.Debug;
                case LogLevel.Debug:
                    LoggingRouter.LogDebug?.Invoke(message);
                    goto case LogLevel.Information;
                case LogLevel.Information:
                    LoggingRouter.LogInformation?.Invoke(message);
                    goto case LogLevel.Warning;
                case LogLevel.Warning:
                    LoggingRouter.LogWarning?.Invoke(message);
                    goto case LogLevel.Error;
                case LogLevel.Error:
                    LoggingRouter.LogError?.Invoke(message);
                    goto case LogLevel.Critical;
                case LogLevel.Critical:
                    LoggingRouter.LogCritical?.Invoke(message);
                    break;
            }
            LoggingRouter.LogInformation?.Invoke(message);
        }

    }
}
