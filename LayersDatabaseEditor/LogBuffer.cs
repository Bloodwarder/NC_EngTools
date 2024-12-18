using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;

namespace LayersDatabaseEditor
{
    public class LogBuffer : INotifyPropertyChanged
    {
        private string? messageContent;
        public event PropertyChangedEventHandler? PropertyChanged;
        private static LogBuffer _instance = null!;
        public static LogBuffer Instance => _instance ??= new LogBuffer();

        private LogBuffer() { }
        public string? MessageContent
        {
            get => messageContent;
            set
            {
                messageContent = value;
                PropertyChanged?.Invoke(_instance, new(nameof(MessageContent)));
            }
        }

        public async Task Message(Run run, Task<string> task)
        {
            Binding binding = new()
            {
                Source = this,
                Path = new(nameof(MessageContent)),
                Mode = BindingMode.OneTime
            };
            run.SetBinding(Run.TextProperty, binding);
            await task;
            run.Text = task.Result;
        }
    }

}
