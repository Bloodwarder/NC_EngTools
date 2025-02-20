using LoaderCore.NanocadUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Windows.Documents;
using System.Windows.Media;

namespace LayersDatabaseEditor.Utilities
{
    public class EditorWindowLogger : ILogger
    {
        private readonly LogLevel _logLevel;
        private DatabaseEditorWindow? _editorWindow;
        private Dictionary<LogLevel, Brush>? _runColorDictionary;

        internal EditorWindowLogger()
        {
            _logLevel = LogLevel.Information;
        }

        public EditorWindowLogger(IConfiguration configuration)
        {
            _logLevel = configuration.GetValue<LogLevel>("Logging:EditorLogLevel");
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;


        public bool IsEnabled(LogLevel logLevel) => logLevel >= _logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_editorWindow != null && IsEnabled(logLevel))
            {
                try
                {
                    var timeRun = new Run($"[{DateTime.Now.ToLongTimeString()}]");
                    timeRun.Foreground = _runColorDictionary![logLevel];

                    var messageRun = new Run($"\t{formatter(state, exception)}");
                    messageRun.Foreground = _runColorDictionary![logLevel];
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(timeRun);
                    paragraph.Inlines.Add(messageRun);
                    _editorWindow?.fdLog.Blocks.Add(paragraph);
                }
                catch (AccessViolationException ex)
                {
                    string str = ex.Message;
                }
            }
        }
        internal void RegisterWindow(DatabaseEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
            _runColorDictionary = new()
            {
                [LogLevel.Trace] = new SolidColorBrush(Colors.Black),
                [LogLevel.Debug] = new SolidColorBrush(Colors.SeaGreen),
                [LogLevel.Information] = editorWindow.fdLog.Foreground,
                [LogLevel.Warning] = new SolidColorBrush(Colors.Olive),
                [LogLevel.Error] = new SolidColorBrush(Colors.DarkRed),
                [LogLevel.Critical] = new SolidColorBrush(Colors.Red)
            };
        }
    }
}
