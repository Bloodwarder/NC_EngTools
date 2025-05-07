using LayersDatabaseEditor.Utilities;
using LayersDatabaseEditor.ViewModel.Ordering;
using System.Windows;
using System.Windows.Controls;

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для GroupOrderingWindow.xaml
    /// </summary>
    public partial class GroupOrderingWindow : Window
    {
        private const int DebounceMs = 400;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(GroupOrderingWindowVm), typeof(GroupOrderingWindow), new PropertyMetadata());

        private readonly Debouncer _debouncer;

        public GroupOrderingWindow(GroupOrderingWindowVm viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            _debouncer = new(() => ViewModel.LayersView.Refresh(), DebounceMs);
#if !DEBUG
            miImportOldIndexes.Visibility = Visibility.Collapsed;
            miTest.Visibility = Visibility.Collapsed;            
#endif
        }

        public GroupOrderingWindowVm ViewModel
        {
            get => (GroupOrderingWindowVm)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

#pragma warning disable IDE1006 // Стили именования
        private void chbInclude_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            bool isChecked = checkBox.IsChecked ?? false;
            var selected = dgLayers.SelectedItems.Cast<LayerGroupedVm>();
            ViewModel.ChangeLayerIncludeState(isChecked, selected);
        }

        private void rbGroupedLayers_Checked(object sender, RoutedEventArgs e)
            => ViewModel.DisplayedLayersState = DisplayedLayers.Grouped;

        private void rbAllLayers_Checked(object sender, RoutedEventArgs e)
            => ViewModel.DisplayedLayersState = DisplayedLayers.All;

        private void rbUngroupedLayers_Checked(object sender, RoutedEventArgs e)
            => ViewModel.DisplayedLayersState = DisplayedLayers.Ungrouped;

        private void tbLayersTextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Trigger();
        }

        private void miTest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void miQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void miImportOldIndexes_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportIndexFromDrawProperties();
        }
#pragma warning restore IDE1006 // Стили именования
    }
}
