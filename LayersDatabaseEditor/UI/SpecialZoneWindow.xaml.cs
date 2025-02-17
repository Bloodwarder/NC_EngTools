using LayersDatabaseEditor.ViewModel.Ordering;
using LayersDatabaseEditor.ViewModel.Zones;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
