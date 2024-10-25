using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public static class LoggingRouter
    {
        public static Action<string>? LogTrace { get; set; }
        public static Action<string>? LogDebug { get; set; }
        public static Action<string>? LogInformation { get; set; }
        public static Action<string>? LogWarning { get; set; }
        public static Action<string>? LogError{ get; set; }
        public static Action<string>? LogCritical { get; set; }


        public static void RegisterLogMethod(LogLevel logLevel, Action<string> action)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    LogTrace += action;
                    LogDebug += action;
                    LogInformation += action;
                    LogWarning += action;
                    LogError += action;
                    LogCritical += action;
                    break;
                case LogLevel.Debug:
                    LogDebug += action;
                    LogInformation += action;
                    LogWarning += action;
                    LogError += action;
                    LogCritical += action;
                    break;
                case LogLevel.Information:
                    LogInformation += action;
                    LogWarning += action;
                    LogError += action;
                    LogCritical += action;
                    break;
                case LogLevel.Warning:
                    LogWarning += action;
                    LogError += action;
                    LogCritical += action;
                    break;
                case LogLevel.Error:
                    LogError += action;
                    LogCritical += action;
                    break;
                case LogLevel.Critical:
                    LogCritical += action;
                    break;
            }
        }

        public static void ClearLoggers()
        {
            LogTrace = null;
            LogDebug = null;
            LogInformation = null;
            LogWarning = null;
            LogError = null;
            LogCritical = null;
        }
    }
}
