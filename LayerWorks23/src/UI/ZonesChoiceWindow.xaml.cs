using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            EnabledZones = zoneLayerNames.ToHashSet();
            foreach (string zoneLayerName in zoneLayerNames)
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

        private void zonesChoiceWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }
    }
}
