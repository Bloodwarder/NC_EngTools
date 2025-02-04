using LayersDatabaseEditor.ViewModel.Ordering;
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
    /// Логика взаимодействия для OrderingWindow.xaml
    /// </summary>
    public partial class OrderingWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(OrderingWindowViewModel), typeof(OrderingWindow), new PropertyMetadata());


        public OrderingWindow(OrderingWindowViewModel viewModel)
        {
            InitializeComponent();
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

        //private void DataGridCell_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    DataGridCell? cell = sender as DataGridCell;
        //    if (cell != null)
        //    {
        //        if (e.Key == Key.Enter)
        //        {
                    
                    
        //            e.Handled = true;
        //        }
        //    }
        //}
    }
}