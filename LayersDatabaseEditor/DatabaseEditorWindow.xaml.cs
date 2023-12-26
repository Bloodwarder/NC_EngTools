using System.Windows;
using System.Windows.Controls;

using LoaderCore;
using LoaderCore.Utilities;
using LayersIO.Excel;

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

        private void LogWrite(string message)
        {
            tbLog.Text += $"\n{message}";
        }

        private void LogClear(string message) 
        {
            tbLog.Text = "";
        }

        private void miExportLayersFromExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelLayerReader.ReadWorkbook(PathProvider.GetPath("Layer_Props.xlsm"));
        }
    }
}
