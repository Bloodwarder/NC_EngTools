using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LoaderCore.Controls.Utilities
{
    public class RgbConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var r = System.Convert.ToByte(values[0]);
                var g = System.Convert.ToByte(values[1]);
                var b = System.Convert.ToByte(values[2]);
                return Color.FromRgb(r, g, b);
            }
            catch
            {
                return Color.FromRgb(127, 127, 127);
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            try
            {
                var color = (Color)value;
                return new object[] { color.R, color.G, color.B };
            }
            catch
            {
                return new object[] { 127, 127, 127 };
            }
        }
    }
}
