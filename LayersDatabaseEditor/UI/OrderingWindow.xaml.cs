using LayersDatabaseEditor.ViewModel.Ordering;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для OrderingWindow.xaml
    /// </summary>
    public partial class OrderingWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(OrderingWindowViewModel), typeof(OrderingWindow), new PropertyMetadata());


        public OrderingWindow(OrderingWindowViewModel viewModel)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ViewModel = viewModel;
        }

        public OrderingWindowViewModel ViewModel
        {
            get { return (OrderingWindowViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void miQuit_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsUpdated)
                ViewModel.ResetValues();
            this.Close();
        }


        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //DataGridCell? cell = sender as DataGridCell;
            //if (cell != null)
            //{
            //    if (e.Key == Key.Enter)
            //    {
            //        cell.IsEditing = false;
            //        var itemVm = (OrderedItemViewModel)cell.DataContext;
            //        PropertyChangedEventHandler? handler = null;
            //        handler = (s, e) =>
            //        {
            //            ViewModel.ItemsView.Refresh();
            //            ((OrderedItemViewModel)sender).PropertyChanged -= handler;
            //        };
            //        itemVm.PropertyChanged += handler;
            //        e.Handled = true;
            //    }
            //}
        }
    }
}