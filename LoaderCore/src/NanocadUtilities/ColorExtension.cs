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
                color = color.ShiftColor(IncreaseChannel, value);
            }
            else if (value < 0)
            {
                color = color.ShiftColor(DecreaseChannel, value);
            }
            return color;
        }

        /// <summary>
        /// Apply function to each of RGB channels
        /// </summary>
        /// <param name="color"></param>
        /// <param name="shiftExpression"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Color ShiftColor (this Color color, Func<byte, double, byte> shiftExpression, double value)
        {
            return Color.FromRgb(shiftExpression(color.Red, value),
                                 shiftExpression(color.Green, value),
                                 shiftExpression(color.Blue, value));
        }
        private static byte IncreaseChannel(byte channel, double value) => (byte)(channel + (255 - channel) * value);
        private static byte DecreaseChannel(byte channel, double value) => (byte)(channel + channel * value);
    }
}
