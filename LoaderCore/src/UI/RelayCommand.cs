using System;
using System.Windows.Input;

namespace LoaderCore.UI
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
            CommandManager.RequerySuggested += (sender, e) => RaiseCanExecuteChanged();
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _execute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand where T : class
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = obj => execute((T)obj!);
            _canExecute = obj => canExecute((T)obj!);
            CommandManager.RequerySuggested += (sender, e) => RaiseCanExecuteChanged();
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _execute == null || (parameter is T && _canExecute(parameter));
        }

        public bool CanExecute(T parameter)
        {
            return _execute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            _execute(parameter);
        }

        public void Execute(T parameter)
        {
            _execute(parameter);
        }
        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}