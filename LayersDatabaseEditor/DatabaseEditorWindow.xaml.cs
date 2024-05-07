using System.Windows;
using System.Windows.Controls;

using LoaderCore;
using LoaderCore.Utilities;
using LayersIO.Excel;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Data;
using System.ComponentModel;

namespace LayersDatabaseEditor
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class DatabaseEditorWindow : Window
    {
        public DatabaseEditorWindow()
        {
            LoaderExtension.InitializeAsLibrary();
            InitializeComponent();
            Logger.WriteLog += LogWrite;
        }

        private async void miTestRun_Click(object sender, RoutedEventArgs e)
        {
            Logger.WriteLog?.Invoke("Запущена тестовая команда");
            Task<string> task = TestMethod1Async();
            await LogWriteAsync(task);

        }

        private static async Task<string> TestMethod1Async()
        {
            await Task.Delay(3000);
            return "Test1 completed";
        }

        private void expLog_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height -= 175d;
            ((Expander)sender).Height = 25d;
        }

        private void expLog_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).Height = 200d;
            this.Height += 175d;
        }

        private async Task LogWriteAsync(Task<string> task)
        {
            Run run = new();
            fdLog.Blocks.Add(new Paragraph(run) { Margin = new(0d) });
            await LogBuffer.Instance.Message(run, task);

            //await Task.Run(() => fdLog.Blocks.Add(new Paragraph(new Run(message))));
        }
        private void LogWrite(string message)
        {
            fdLog.Blocks.Add(new Paragraph(new Run(message)) { Margin = new(0d) });
        }

        private void LogClear(string message)
        {
            fdLog.Blocks.Clear();
        }

        private async void miExportLayersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            LogWrite("Запущен импорт слоёв из Excel");
            Task<string> task = ExcelLayerReader.ReadWorkbookAsync(PathProvider.GetPath("Layer_Props.xlsm"));
            await LogWriteAsync(task);
            //ExcelLayerReader.ReadWorkbook(PathProvider.GetPath("Layer_Props.xlsm"));
        }

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
}
