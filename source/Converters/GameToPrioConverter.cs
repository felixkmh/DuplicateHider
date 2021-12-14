using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DuplicateHider.Converters
{
    class GameToPrioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Game game)
            {
                var copies = DuplicateHiderPlugin.Instance.GetCopies(game);
                var index = copies.IndexOf(game);
                if (targetType == typeof(string))
                {
                    return index.ToString();
                }
                return index;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
