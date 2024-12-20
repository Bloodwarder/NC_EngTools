using LayersIO.DataTransfer;
using System.Globalization;
using System.Windows.Data;

namespace LayersDatabaseEditor.Utilities
{
    public class EnumToItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.GetValues(typeof(DrawTemplate)).Cast<DrawTemplate>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
