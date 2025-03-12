using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.SharedData;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoMod.UI
{
    /// <summary>
    /// Логика взаимодействия для ZonesChoiceWindow.xaml
    /// </summary>
    public partial class ZoneDiffValuesWindow : Window
    {
        //IRepository<string, ZoneInfo>? _repository;
        public ZoneDiffValuesWindow(IEnumerable<string> zoneLayerNames)
        {
            //_repository = NcetCore.ServiceProvider.GetService<IRepository<string, ZoneInfo>>();
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var zones = zoneLayerNames.OrderBy(x => x).Select(s => new ZoneValue(s));
            Zones = new(zones);
            DataContext = this;
        }

        public ObservableCollection<ZoneValue> Zones { get; }

        //private void chbIsActivated_Click(object sender, RoutedEventArgs e)
        //{
        //    CheckBox checkBox = (CheckBox)sender;
        //    if (checkBox.IsChecked!.Value)
        //    {
        //        EnabledZones.Add((string)dgZoneDiffValues.SelectedItem);
        //    }
        //    else
        //    {
        //        EnabledZones.Remove((string)dgZoneDiffValues.SelectedItem);
        //    }
        //}

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void zoneDiffValuesWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            bool isNullOrMainFocused = focusedElement == this || focusedElement == null;
            if (isNullOrMainFocused && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void svZoneDiffValues_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void zoneDiffValuesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += zoneDiffValuesWindow_PreviewKeyDown;
        }

        public class ZoneValue
        {
            public ZoneValue(string layerName)
            {
                Layer = layerName;
                Value = 0; // Переделать на значение по умолчанию. Парсер в другой сборке - решить проблему
            }
            public string Layer { get; }
            public double Value { get; }

        }
    }
}
