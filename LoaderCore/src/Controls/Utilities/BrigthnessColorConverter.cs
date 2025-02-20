using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LoaderCore.Controls.Utilities
{
    public class BrigthnessColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var basecolor = values[0] as Color?;
            var br = Math.Clamp((double)values[1], -1d, 1d);
            if (basecolor == null)
                return Color.FromRgb(127, 127, 127);
            var r = (byte)(br > 0 ? basecolor.Value.R + (255 - basecolor.Value.R) * br : basecolor.Value.R + br * basecolor.Value.R);
            var g = (byte)(br > 0 ? basecolor.Value.G + (255 - basecolor.Value.G) * br : basecolor.Value.G + br * basecolor.Value.G);
            var b = (byte)(br > 0 ? basecolor.Value.B + (255 - basecolor.Value.B) * br : basecolor.Value.B + br * basecolor.Value.B);

            return Color.FromRgb(r, g, b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
