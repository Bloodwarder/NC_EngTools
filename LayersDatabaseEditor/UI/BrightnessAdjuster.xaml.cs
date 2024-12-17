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
    public partial class BrightnessAdjuster : UserControl
    {
        public static readonly DependencyProperty BaseColorProperty;

        static BrightnessAdjuster()
        {
            BaseColorProperty = DependencyProperty.Register("BaseColor", typeof(Color), typeof(BrightnessAdjuster));
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
    }
}
