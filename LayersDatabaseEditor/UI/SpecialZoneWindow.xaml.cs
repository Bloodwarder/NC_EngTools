using LayersDatabaseEditor.ViewModel.Zones;
using System.ComponentModel;
using System.Windows;

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для SpecialZoneWindow.xaml
    /// </summary>
    public partial class SpecialZoneWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(SpecialZoneEditorVm), typeof(SpecialZoneWindow), new PropertyMetadata());

        public SpecialZoneWindow(SpecialZoneEditorVm viewModel)
        {
            InitializeComponent();
#if !DEBUG
            miTestButton.Visibility = Visibility.Collapsed;
#endif
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ViewModel = viewModel;
        }

        public SpecialZoneEditorVm ViewModel
        {
            get { return (SpecialZoneEditorVm)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = dgSpecialZones.SelectedItem;
            //var selectedRow = (DataGridRow)dgSpecialZones.ItemContainerGenerator.ContainerFromItem(item);
            //var column = (DataGridComboBoxColumn)dgSpecialZones.Columns[2];
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {

                this.Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (ViewModel.IsUpdated)
            {
                MessageBoxResult result = 
                    MessageBox.Show("Список специальных зон изменён. Записать изменения в базу данных?", "Записать изменения", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        ViewModel.UpdateDatabaseEntities();
                        break;
                    case MessageBoxResult.No:
                        ViewModel.ResetValues();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
        }
    }
}
