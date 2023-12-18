using System.Windows;
using System.Windows.Controls;

using Loader;
using Loader.CoreUtilities;

using LayersDatabaseEditor.DatabaseInteraction;

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
            TestRun.RunTest(this);
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
    }
}
