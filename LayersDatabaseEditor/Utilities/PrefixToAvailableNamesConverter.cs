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
            string prefix = (string)values[0];
            IEnumerable<SimpleLayer> collection = (IEnumerable<SimpleLayer>)values[1];
            return collection.Where(l => l.Prefix == prefix).OrderBy(l => l.MainName).AsEnumerable();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
