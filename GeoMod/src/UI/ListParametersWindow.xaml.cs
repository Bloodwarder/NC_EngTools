using LoaderCore;
using LoaderCore.Interfaces;
using LoaderCore.SharedData;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoMod.UI
{
    /// <summary>
    /// Логика взаимодействия для ListParametersWindow.xaml
    /// </summary>
    public partial class ListParametersWindow : Window, INotifyPropertyChanged
    {
        //IRepository<string, ZoneInfo>? _repository;
        public ListParametersWindow(IEnumerable<string> parameterNames)
        {
            //_repository = NcetCore.ServiceProvider.GetService<IRepository<string, ZoneInfo>>();
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var parameters = parameterNames.OrderBy(x => x).Select(s => new ParameterObject(s));
            Parameters = new(parameters);
            PropertyChanged?.Invoke(this, new(nameof(Parameters)));
            DataContext = this;
        }

        public ObservableCollection<ParameterObject> Parameters { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

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

#pragma warning disable IDE1006 // Стили именования
        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void listParametersWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            bool isNullOrMainFocused = focusedElement == this || focusedElement == null;
            if (isNullOrMainFocused && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void svParameters_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void listParametersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += listParametersWindow_PreviewKeyDown;
        }

        public class ParameterObject
        {
            public ParameterObject(string parameterName, double value = 0)
            {
                Parameter = parameterName;
                Value = value; // Переделать на значение по умолчанию. Парсер в другой сборке - решить проблему
            }
            public string Parameter { get; set; }
            public double Value { get; set; }

        }
#pragma warning restore IDE1006 // Стили именования
    }
}
