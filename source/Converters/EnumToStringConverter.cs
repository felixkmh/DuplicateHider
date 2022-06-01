using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DuplicateHider.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ListSortDirection sortDirection)
            {
                switch(sortDirection)
                {
                    case ListSortDirection.Ascending:
                        return ResourceProvider.GetString("LOCMenuSortAscending");
                    case ListSortDirection.Descending:
                        return ResourceProvider.GetString("LOCMenuSortDescending");
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
