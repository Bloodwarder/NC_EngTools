using LayersDatabaseEditor.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LayersDatabaseEditor.Utilities
{
    public class PrefixToAvailableNamesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not string prefix || values[1] is not IEnumerable<SimpleLayer> collection)
                return Array.Empty<string>();
            return collection.Where(l => l.Prefix == prefix).Select(l => l.MainName).OrderBy(l => l).AsEnumerable();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
