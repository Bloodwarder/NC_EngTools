using System.ComponentModel;
using System.Globalization;

namespace LayerWorks.EntityFormatters
{
    internal class WindowsColorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(NameClassifiers.SharedProperties.Color))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value.GetType() == typeof(NameClassifiers.SharedProperties.Color))
            {
                var color = (NameClassifiers.SharedProperties.Color)value;
                return System.Windows.Media.Color.FromRgb(color.Red, color.Green, color.Blue);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
