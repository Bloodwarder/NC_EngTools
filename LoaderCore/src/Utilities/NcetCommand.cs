using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Logging;
using System;

namespace LoaderCore.Utilities
{
    public class NcetCommand
    {
        Action _commandAction;

        public NcetCommand(Action commandAction)
        {
            _commandAction = commandAction;
        }

        public static void ExecuteCommand(Action commandAction)
        {
            var command = new NcetCommand(commandAction);
            command.Execute();
        }

        public void Execute()
        {
            Workstation.Define();
            try
            {
                _commandAction.Invoke();
            }
            catch (Exception ex)
            {
                NcetCore.Logger!.LogError(ex, "Команда не выполнена. Ошибка: {ExceptionMessage}", ex.Message);
            }
        }
    }
}
