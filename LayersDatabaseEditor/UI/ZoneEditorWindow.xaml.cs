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
        public static readonly DependencyProperty ViewModelProperty;
        private string _lastSearch = string.Empty;
        
        static ZoneEditorWindow()
        {
            ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ZoneEditorViewModel), typeof(ZoneEditorWindow));
        }

        public ZoneEditorWindow(ZoneEditorViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            DataGridColumn sourceLayerColumn = dgZones.Columns.Single(c => c.Header.ToString() == "Слой источник");
            DataGridColumn zoneLayerColumn = dgZones.Columns.Single(c => c.Header.ToString() == "Слой зон");
            sourceLayerColumn.Visibility = ViewModel.IsSourceVisible;
            zoneLayerColumn.Visibility = ViewModel.IsZoneVisible;

            CollectionViewSource = (CollectionViewSource)Resources["zonesViewSource"];

            //Binding b = new("Zones");
            //b.Source = viewModel;
            //b.Mode = BindingMode.TwoWay;
            //BindingOperations.SetBinding(CollectionViewSource, CollectionViewSource.SourceProperty, b);

            CollectionViewSource.Filter += new FilterEventHandler(FilterCallback);
            CollectionViewSource.LiveFilteringProperties.Add(nameof(ZoneGroupInfoViewModel.SourceLayerName));
            
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
            string search = inputFilter.inputText.Text;

            if (string.IsNullOrEmpty(search))
            {
                e.Accepted = true;
                return;
            }

            //if (_lastSearch == search)
            //{
            //    e.Accepted = CollectionViewSource.View.Contains(e.Item); // тоже спорно - внутри ListCollectionView без хэша
            //    return;
            //}

            if (e.Item is ZoneGroupInfoViewModel item && item.SourceLayerName.Contains(search))
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
            //Dispatcher.BeginInvoke(() => CollectionViewSource.View.Refresh()); // Вызывает фильтр заново. Сделано, чтобы кнопки апдейтили вид чекбоксов. Найти другой способ их апдейтить
            dgZones.Items.Refresh();
        }

        private void chbIgnoreWidth_Click(object sender, RoutedEventArgs e)
        {
            if (dgZones.SelectedItems.Count > 1)
            {
                var checkBox = (CheckBox)sender;
                UpdateZoneInfoViewModel(zi => zi.IgnoreConstructionWidth = checkBox.IsChecked ?? false);
                e.Handled = true;
                RefreshDataGrid(sender, e);
            }
        }

        private void chbIsActivated_Click(object sender, RoutedEventArgs e)
        {
            if (dgZones.SelectedItems.Count > 1)
            {
                var checkBox = (CheckBox)sender;
                UpdateZoneInfoViewModel(zi => zi.IsActivated = checkBox.IsChecked ?? false);
                e.Handled = true;
                RefreshDataGrid(sender, e);
            }
        }
        private void UpdateZoneInfoViewModel(Action<ZoneGroupInfoViewModel> action)
        {
            var updatedItems = dgZones.SelectedItems.Cast<ZoneGroupInfoViewModel>();
            foreach (var item in updatedItems)
                action(item);
        }
    }
}
