using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetCommand : IDisposable
    {
        private Action _commandAction;
        private IDisposable _commandScope;

        public NcetCommand(Action commandAction)
        {
            _commandAction = commandAction;
        }

        private ILogger? Logger { get; set; }
        private IDisposable? Scope;
        public static void ExecuteCommand(Action commandAction)
        {
            using (var command = new NcetCommand(commandAction))
            {
                if (Workstation.IsCommandLoggingEnabled)
                {
                    var logger = NcetCore.ServiceProvider.GetRequiredService<ILogger<NcetCommand>>();
                    command.Scope = logger.BeginScope(commandAction.Method.Name);
                    command.Logger = logger;
                }
                command.Execute();
            }
        }

        public void Dispose()
        {
            this.Logger?.LogInformation("Завершение лога команды");
            Scope?.Dispose();
            Workstation.Logger = NcetCore.Logger;
            Workstation.IsCommandLoggingEnabled = false;
        }

        public void Execute()
        {
            if (Logger != null)
                Workstation.Define(Logger);
            else
                Workstation.Define();
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
