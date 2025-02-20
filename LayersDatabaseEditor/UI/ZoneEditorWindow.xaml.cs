using LayersDatabaseEditor.ViewModel.Zones;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для ZoneLayersAssignWindow.xaml
    /// </summary>
    public partial class ZoneEditorWindow : Window
    {
        private const int DebounceMillisecondsTimeout = 400;
        public static readonly DependencyProperty ViewModelProperty;
        private string _lastSearch = string.Empty;
        private Task? _debounceTimeoutTask;
        private CancellationTokenSource _cts = null!;
        private Func<ZoneGroupInfoVm, string, bool> _filterPredicate;

        static ZoneEditorWindow()
        {
            ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ZoneEditorVm), typeof(ZoneEditorWindow));
        }

        public ZoneEditorWindow(ZoneEditorVm viewModel)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            ViewModel = viewModel;

            InitializeComponent();

            DataGridColumn sourceLayerColumn = dgZones.Columns.Single(c => c.Header.ToString() == "Слой источник");
            DataGridColumn zoneLayerColumn = dgZones.Columns.Single(c => c.Header.ToString() == "Слой зон");
            sourceLayerColumn.Visibility = ViewModel.IsSourceVisible;
            zoneLayerColumn.Visibility = ViewModel.IsZoneVisible;
            _filterPredicate = ViewModel.IsSourceVisible == Visibility.Visible ? (zg, search) => zg.SourceLayerName.Contains(search) : (zg, search) => zg.ZoneLayerName.Contains(search);

            CollectionViewSource = (CollectionViewSource)Resources["zonesViewSource"];

            CollectionViewSource.Filter += new FilterEventHandler(FilterCallback);
            CollectionViewSource.LiveFilteringProperties.Add(nameof(ZoneGroupInfoVm.SourceLayerName));

            inputFilter.TextChanged += (s, e) =>
            {
                if (_debounceTimeoutTask?.Status == TaskStatus.Running)
                {
                    _cts.Cancel();
                }
                _cts = new();
                _debounceTimeoutTask =  Task.Run(() =>
                {
                    Task.Delay(DebounceMillisecondsTimeout, _cts.Token).Wait();
                    Dispatcher.Invoke(() =>
                    {
                        CollectionViewSource.View.Refresh();
                        _lastSearch = inputFilter.inputText.Text;
                    }
                    , DispatcherPriority.Background);
                }, _cts.Token);
            };
        }

        public ZoneEditorVm ViewModel
        {
            get => (ZoneEditorVm)GetValue(ViewModelProperty);
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

            if (e.Item is ZoneGroupInfoVm item && _filterPredicate(item, search))
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
        private void UpdateZoneInfoViewModel(Action<ZoneGroupInfoVm> action)
        {
            var updatedItems = dgZones.SelectedItems.Cast<ZoneGroupInfoVm>();
            foreach (var item in updatedItems)
                action(item);
        }

        private void svZones_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
