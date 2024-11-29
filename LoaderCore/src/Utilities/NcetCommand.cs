using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetCommand : IDisposable
    {
        private readonly Action _commandAction;

        internal NcetCommand(Action commandAction)
        {
            Workstation.Define();
            _commandAction = commandAction;
        }

        public string Name => _commandAction.Method.Name;
        private IDisposable? Scope { get; set; }
        public static void ExecuteCommand(Action commandAction)
        {
            using (var command = new NcetCommand(commandAction))
            {
                if (Workstation.IsCommandLoggingEnabled)
                {
                    var logger = NcetCore.ServiceProvider.GetRequiredService<ILogger<NcetCommand>>();
                    command.Scope = logger.BeginScope(command.Name);
                    Workstation.SetLogger(logger);
                    Workstation.Logger?.LogInformation("Лог команды\nИмя метода:\t{CommandName}\nДата:\t{Date}\nВремя начала:\t{Time}\nПользователь:\t{UserName}",
                                                      command.Name,
                                                      DateTime.Now.ToLongDateString(),
                                                      DateTime.Now.ToLongTimeString(),
                                                      Environment.UserName);
                }
                command.Execute();
            }
        }

        public void Dispose()
        {
            Scope?.Dispose();
            Workstation.Logger = NcetCore.Logger;
            Workstation.IsCommandLoggingEnabled = false;
            GC.SuppressFinalize(this);
        }

        internal void Execute()
        {
            try
            {
                _commandAction.Invoke();
            }
            catch (Exception ex)
            {
                Workstation.Logger?.LogError(ex, "Команда не выполнена. Ошибка: {ExceptionMessage}", ex.Message);
                Workstation.Logger?.LogTrace(ex,
                                             "{ProcessingObject}: Вывод трассировки стека:\n{StackTrace}",
                                             nameof(NcetCommand),
                                             ex.StackTrace);
            }
        }
    }
}
