using HostMgd.ApplicationServices;
using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetEditorConsoleLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;


        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

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
                    // TODO: понять из-за чего прилетает и обработать
                }
            }
        }

    }
}
