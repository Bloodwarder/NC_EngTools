using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace LayerWorks.EntityFormatters
{
    internal class TeighaColorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(NameClassifiers.SharedProperties.Color))
                return true;
            if (sourceType == typeof(System.Drawing.Color))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value.GetType() == typeof(NameClassifiers.SharedProperties.Color))
            {
                var color = (NameClassifiers.SharedProperties.Color)value;
                return Teigha.Colors.Color.FromRgb(color.Red, color.Green, color.Blue);
            }
            if (value.GetType() == typeof(System.Drawing.Color))
            {
                var color = (System.Drawing.Color)value;
                return Teigha.Colors.Color.FromColor(color);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
