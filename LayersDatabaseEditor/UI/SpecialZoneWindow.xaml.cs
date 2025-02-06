using LayersDatabaseEditor.ViewModel.Ordering;
using LayersDatabaseEditor.ViewModel.Zones;
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
    /// Логика взаимодействия для SpecialZoneWindow.xaml
    /// </summary>
    public partial class SpecialZoneWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(SpecialZoneEditorViewModel), typeof(SpecialZoneWindow), new PropertyMetadata());

        public SpecialZoneWindow(SpecialZoneEditorViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
        }

        public SpecialZoneEditorViewModel ViewModel
        {
            get { return (SpecialZoneEditorViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}
