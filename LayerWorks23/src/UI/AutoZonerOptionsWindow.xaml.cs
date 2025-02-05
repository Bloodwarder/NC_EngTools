using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LayerWorks.UI
{
    /// <summary>
    /// Логика взаимодействия для AutoZonerOptionsWindow.xaml
    /// </summary>
    public partial class AutoZonerOptionsWindow : Window
    {

        public AutoZonerOptionsWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;

        }

        public bool IsZoneChoiceNeeded { get; set; } = false;
        public bool IgnoreLabelRecognition { get; set; } = false;
        public bool CalculateSinglePipe { get; set; } = false;

        private void autoZonerOptionsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            if ((e.Key == Key.Enter || e.Key == Key.Escape) && focusedElement is not CheckBox)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.Close();
        }

        private void autoZonerOptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += autoZonerOptionsWindow_PreviewKeyDown;
        }

        private void CheckBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                var checkBox = (CheckBox)sender;
                checkBox.IsChecked = !checkBox.IsChecked;
            }
        }
    }
}
