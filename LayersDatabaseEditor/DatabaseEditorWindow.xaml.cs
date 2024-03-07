using System.Windows;
using System.Windows.Controls;

using LoaderCore;
using LoaderCore.Utilities;
using LayersIO.Excel;
using System.Windows.Documents;
using System.Diagnostics;

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

        private void miTestRun_Click(object sender, RoutedEventArgs e)
        {
            Logger.WriteLog("Запущена тестовая команда");
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

        private async Task LogWriteAsync(string message)
        {
            await Task.Run(() => fdLog.Blocks.Add(new Paragraph(new Run(message))));
        }
        private void LogWrite(string message)
        {
            fdLog.Blocks.Add(new Paragraph(new Run(message)));
        }

        private void LogClear(string message)
        {
            fdLog.Blocks.Clear();
        }

        private void miExportLayersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelLayerReader.ReadWorkbook(PathProvider.GetPath("Layer_Props.xlsm"));
        }
    }
}
