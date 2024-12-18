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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LayersDatabaseEditor.UI
{
    /// <summary>
    /// Логика взаимодействия для ColorAdjuster.xaml
    /// </summary>
    public partial class ColorAdjuster : LabeledHorizontalInput
    {
        public static readonly DependencyProperty ColorProperty;
        static ColorAdjuster()
        {
            ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorAdjuster));
        }
        public ColorAdjuster()
        {
            InitializeComponent();
            Binding binding = new();
            binding.ElementName = "baseColorRectangle";
            binding.Path = new PropertyPath("Fill.Color");
            binding.Mode = BindingMode.TwoWay;
            this.SetBinding(ColorProperty, binding);
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }


        private void Sliders_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Slider slider = (Slider)sender;
            slider.Value -= e.Delta / 120d * slider.SmallChange;
            e.Handled = true;
        }
    }
}
