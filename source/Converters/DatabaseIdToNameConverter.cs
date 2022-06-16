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
    public class DatabaseIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Guid id)
            {
                var api = DuplicateHiderPlugin.Instance.PlayniteApi;
                {
                    if (api.Database.AgeRatings.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Categories.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Companies.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Features.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Genres.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Platforms.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Regions.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Series.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
                {
                    if (api.Database.Tags.Get(id) is DatabaseObject databaseObject)
                    {
                        return databaseObject.Name;
                    }
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
