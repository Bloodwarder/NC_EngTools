using System;
using System.Windows.Threading;

namespace LayersDatabaseEditor.Utilities
{
    internal class Debouncer
    {
        private readonly DispatcherTimer _timer;
        private readonly Action _action;
        private readonly int _debounceMs;

        public Debouncer(Action action, int debounceMs, DispatcherPriority priority = DispatcherPriority.Background)
        {
            _debounceMs = debounceMs;
            _action = action;
            
            _timer = new(priority, Dispatcher.CurrentDispatcher) 
            { 
                Interval = TimeSpan.FromMilliseconds(_debounceMs),
            };

            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                _action?.Invoke();
            };
        }

        public void Trigger()
        {
            _timer.Stop();
            _timer.Start();
        }
    }
}
