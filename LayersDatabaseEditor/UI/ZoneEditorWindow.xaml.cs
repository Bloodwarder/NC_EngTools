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

        static ZoneEditorWindow()
        {
            ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ZoneEditorViewModel), typeof(ZoneEditorWindow));
        }

        public ZoneEditorWindow(ZoneEditorViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();

            CollectionViewSource collectionViewSource = (CollectionViewSource)Resources["zonesViewSource"];
            collectionViewSource.Filter += new FilterEventHandler(FilterCallback);
            inputFilter.TextChanged += (s, e) =>
            {
                collectionViewSource.View.Refresh();
            };
        }



        public ZoneEditorViewModel ViewModel
        {
            get => (ZoneEditorViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private void FilterCallback(object sender, FilterEventArgs e)
        {
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
    }
}
