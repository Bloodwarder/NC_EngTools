using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetCommand : IDisposable
    {
        private readonly Action _commandAction;

        public NcetCommand(Action commandAction)
        {
            Workstation.Define();
            _commandAction = commandAction;
        }

        private IDisposable? Scope;
        public static void ExecuteCommand(Action commandAction)
        {
            using (var command = new NcetCommand(commandAction))
            {
                if (Workstation.IsCommandLoggingEnabled)
                {
                    var logger = NcetCore.ServiceProvider.GetRequiredService<ILogger<NcetCommand>>();
                    command.Scope = logger.BeginScope(commandAction.Method.Name);
                    Workstation.SetLogger(logger);
                }
                command.Execute();
            }
        }

        public void Dispose()
        {
            Scope?.Dispose();
            Workstation.Logger = NcetCore.Logger;
            Workstation.IsCommandLoggingEnabled = false;
        }

        internal void Execute()
        {
            try
            {
                _commandAction.Invoke();
            }
            catch (Exception ex)
            {
                Workstation.Logger!.LogError(ex, "Команда не выполнена. Ошибка: {ExceptionMessage}", ex.Message);
            }
        }
    }
}
