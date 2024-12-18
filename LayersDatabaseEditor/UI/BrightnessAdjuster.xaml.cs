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
    /// Логика взаимодействия для BrightnessAdjuster.xaml
    /// </summary>
    public partial class BrightnessAdjuster : LabeledHorizontalInput
    {
        public static readonly DependencyProperty BaseColorProperty;
        public static readonly DependencyProperty BrightnessShiftProperty;

        static BrightnessAdjuster()
        {
            BaseColorProperty = DependencyProperty.Register("BaseColor", typeof(Color), typeof(BrightnessAdjuster));
            BrightnessShiftProperty = DependencyProperty.Register("BrightnessShift", typeof(double), typeof(BrightnessAdjuster));
        }

        public BrightnessAdjuster()
        {
            InitializeComponent();
        }

        public Color BaseColor
        {
            get => (Color)GetValue(BaseColorProperty);
            set => SetValue(BaseColorProperty, value);
        }

        public double BrightnessShift
        {
            get => (double)GetValue(BrightnessShiftProperty);
            set => SetValue(BrightnessShiftProperty, value);
        }

        private void sliderBr_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            BrightnessShift -= e.Delta/120 * sliderBr.SmallChange;
            e.Handled = true;
        }
    }
}
