using LoaderCore.Utilities;
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

namespace LoaderCore.UI
{
    /// <summary>
    /// Логика взаимодействия для ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        public ErrorListWindow(IEnumerable<ErrorEntry> entries, string listName)
        {
            InitializeComponent();
            this.Title = listName;
            foreach (var entry in entries)
            {
                Errors.Add(entry);
            }
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        public ObservableCollection<ErrorEntry> Errors { get; } = new();

        private void errorListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement(this);
            if ((e.Key == Key.Enter || e.Key == Key.Escape) && focusedElement is not CheckBox)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void errorListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += errorListWindow_PreviewKeyDown;
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
    }
}
