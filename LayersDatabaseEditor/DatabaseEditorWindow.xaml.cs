using LayersIO.Excel;
using LoaderCore;
using LoaderCore.Utilities;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LayersIO.Connection;

namespace LayersDatabaseEditor
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class DatabaseEditorWindow : Window
    {
        readonly ILogger? _logger = NcetCore.ServiceProvider.GetService<ILogger>();
        public DatabaseEditorWindow()
        {
            
            InitializeComponent();
            PreInitializeSimpleLogger.Log += LogWrite;
        }

        private async void miTestRun_Click(object sender, RoutedEventArgs e)
        {
            _logger?.LogInformation("Запущена тестовая команда");
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

        private void LogClear()
        {
            fdLog.Blocks.Clear();
        }

        private async void miExportLayersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            LogWrite("Запущен импорт слоёв из Excel");
            var reader = new ExcelLayerReader();
            Task<string> task = reader.ReadWorkbookAsync(PathProvider.GetPath("Layer_Props.xlsm"));
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

        private void miTestRun2_Click(object sender, RoutedEventArgs e)
        {
            using (LayersDatabaseContextSqlite db = new(PathProvider.GetPath("LayerData_ИС.db")))
            {
                var layers = db.Layers.Skip(25).Take(5).ToArray();
                foreach (var layer in layers)
                {
                    LogWrite($"{layer.Name}   {layer.LayerDrawTemplateData?.DrawTemplate}  {layer.LayerPropertiesData?.LinetypeScale}");
                }
            }
        }
    }
}
