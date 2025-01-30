using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LayerWorks.UI
{
    /// <summary>
    /// Логика взаимодействия для ZonesChoiceWindow.xaml
    /// </summary>
    public partial class ZonesChoiceWindow : Window
    {
        public ZonesChoiceWindow(IEnumerable<string> zoneLayerNames)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            EnabledZones = zoneLayerNames.ToHashSet();
            foreach (string zoneLayerName in zoneLayerNames.OrderBy(n => n).AsEnumerable())
                ZoneLayerNames.Add(zoneLayerName);
            DataContext = this;
            dgActiveZones.ItemsSource = ZoneLayerNames;
        }

        public ObservableCollection<string> ZoneLayerNames { get; } = new();
        public HashSet<string> EnabledZones { get; }

        private void chbIsActivated_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            if (checkBox.IsChecked!.Value)
            {
                EnabledZones.Add((string)dgActiveZones.SelectedItem);
            }
            else
            {
                EnabledZones.Remove((string)dgActiveZones.SelectedItem);
            }
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void zonesChoiceWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            bool isNullOrMainFocused = focusedElement == this || focusedElement == null;
            if (isNullOrMainFocused && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void svActiveZones_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void zonesChoiceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += zonesChoiceWindow_PreviewKeyDown;
        }
    }
}
