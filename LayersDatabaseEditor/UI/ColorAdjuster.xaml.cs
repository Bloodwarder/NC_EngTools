using LayersDatabaseEditor.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class ColorAdjuster : LabeledHorizontalInput, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ColorProperty;

        //public static readonly DependencyProperty RedProperty;
        //public static readonly DependencyProperty GreenProperty;
        //public static readonly DependencyProperty BlueProperty;

        public event PropertyChangedEventHandler? PropertyChanged;

        static ColorAdjuster()
        {
            ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorAdjuster), new PropertyMetadata(Color.FromRgb(127, 127, 127)));
            //RedProperty = DependencyProperty.Register("RedProperty", typeof(byte), typeof(ColorAdjuster), new PropertyMetadata(127));
            //GreenProperty = DependencyProperty.Register("GreenProperty", typeof(byte), typeof(ColorAdjuster), new PropertyMetadata(127));
            //BlueProperty = DependencyProperty.Register("BlueProperty", typeof(byte), typeof(ColorAdjuster), new PropertyMetadata(127));

        }
        public ColorAdjuster()
        {
            InitializeComponent();
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);

            set
            {
                SetValue(ColorProperty, value);
                //UpdateRgbControls();
            }
        }

        public byte Red
        {
            get => Color.R;
            set
            {
                Color = Color.FromRgb(value, Green, Blue);
                //OnPropertyChanged(nameof(Color));
            }
        }

        public byte Green
        {
            get => Color.G;
            set
            {
                Color = Color.FromRgb(Red, value, Blue);
                //OnPropertyChanged(nameof(Color));
            }
        }

        public byte Blue
        {
            get => Color.B;
            set
            {
                Color = Color.FromRgb(Red, Green, value);
                //OnPropertyChanged(nameof(Color));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void UpdateRgbControls()
        {
            OnPropertyChanged(nameof(Red));
            OnPropertyChanged(nameof(Green));
            OnPropertyChanged(nameof(Blue));
        }

        private void Sliders_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Slider slider = (Slider)sender;
            slider.Value -= e.Delta / 120d * slider.SmallChange;
            e.Handled = true;
        }

        private void colorAdjuster_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //UpdateRgbControls();
        }

        private void tbRed_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Red = Convert.ToByte(tbRed.Text);
            }
            catch
            {

            }
        }

        private void tbGreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Green = Convert.ToByte(tbGreen.Text);
            }
            catch
            {

            }
        }

        private void tbBlue_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Blue = Convert.ToByte(tbBlue.Text);
            }
            catch
            {

            }
        }
    }
}
