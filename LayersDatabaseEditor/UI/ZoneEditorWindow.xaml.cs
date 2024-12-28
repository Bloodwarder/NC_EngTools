using LayersDatabaseEditor.ViewModel;
using System;
using System.Collections.Generic;
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

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для ZoneLayersAssignWindow.xaml
    /// </summary>
    public partial class ZoneEditorWindow : Window
    {
        public static DependencyProperty ViewModelProperty;
        private string _lastSearch = string.Empty;

        static ZoneEditorWindow()
        {
            ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ZoneEditorViewModel), typeof(ZoneEditorWindow));
        }

        public ZoneEditorWindow(ZoneEditorViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            CollectionViewSource = (CollectionViewSource)Resources["zonesViewSource"];

            //Binding b = new("Zones");
            //b.Source = viewModel;
            //b.Mode = BindingMode.TwoWay;
            //BindingOperations.SetBinding(CollectionViewSource, CollectionViewSource.SourceProperty, b);

            CollectionViewSource.Filter += new FilterEventHandler(FilterCallback);
            inputFilter.TextChanged += (s, e) =>
            {
                var operation = Dispatcher.BeginInvoke(() => CollectionViewSource.View.Refresh());
                operation.Completed += (s, e) => _lastSearch = inputFilter.inputText.Text;
            };
        }



        public ZoneEditorViewModel ViewModel
        {
            get => (ZoneEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        CollectionViewSource CollectionViewSource { get; set; }

        private void FilterCallback(object sender, FilterEventArgs e)
        {
            //if (_lastSearch == inputFilter.inputText.Text)
            //    return;
            if (string.IsNullOrEmpty(inputFilter.inputText.Text))
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is ZoneInfoViewModel item && item.SourceLayerName.Contains(inputFilter.inputText.Text))
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void bExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshDataGrid(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => CollectionViewSource.View.Refresh()); // BUG: Вызывает фильтр заново. Сделано, чтобы кнопки апдейтили вид чекбоксов. Найти другой способ их апдейтить
        }
    }
}
