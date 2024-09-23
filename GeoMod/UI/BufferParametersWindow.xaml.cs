using NetTopologySuite.Operation.Buffer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoMod.UI
{
    /// <summary>
    /// Логика взаимодействия для BufferParametersWindow.xaml
    /// </summary>
    public partial class BufferParametersWindow : Window
    {
        private BufferParametersViewModel _bufferParametersViewModel;
        public BufferParametersWindow(ref BufferParameters bufferParameters)
        {
            _bufferParametersViewModel = new(ref bufferParameters);
            this.DataContext = _bufferParametersViewModel;
            InitializeComponent();
            Binding endCapBinding = new()
            {
                ElementName = "bufferParametersWindow",
                Path = new PropertyPath("DataContext.EndCapStyle"),
                Mode = BindingMode.TwoWay
            };
            cbEndCap.SetBinding(ComboBox.SelectedValueProperty, endCapBinding);
            cbEndCap.ItemsSource = typeof(EndCapStyle).GetEnumValues();
            cbEndCap.SelectedItem = _bufferParametersViewModel.EndCapStyle;

            Binding joinStyleBinding = new()
            {
                ElementName = "bufferParametersWindow",
                Path = new PropertyPath("DataContext.JoinStyle"),
                Mode = BindingMode.TwoWay
            };
            cbJoinStyle.SetBinding(ComboBox.SelectedValueProperty, joinStyleBinding);
            cbJoinStyle.ItemsSource = typeof(JoinStyle).GetEnumValues();
            cbJoinStyle.SelectedItem = _bufferParametersViewModel.JoinStyle;
            this.Closing += _bufferParametersViewModel.SubmitParameters;
        }
    }
}
