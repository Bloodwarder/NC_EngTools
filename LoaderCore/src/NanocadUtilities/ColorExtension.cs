using System;
using Teigha.Colors;

namespace LoaderCore.NanocadUtilities
{
    public static class ColorExtension
    {
        public static Color BrightnessShift(this Color color, double value)
        {
            value = Math.Clamp(value, -1d, 1d);
            if (value == 0)
                return Color.FromColorIndex(ColorMethod.ByLayer, 256);
            if (value > 0)
            {
                color = Color.FromRgb((byte)(color.Red + (255 - color.Red) * value), (byte)(color.Green + (255 - color.Green) * value), (byte)(color.Blue + (255 - color.Blue) * value));
            }
            else if (value < 0)
            {
                color = Color.FromRgb((byte)(color.Red + color.Red * value), (byte)(color.Green + color.Green * value), (byte)(color.Blue + color.Blue * value));
            }
            return color;
        }
    }
}
