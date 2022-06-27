using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DuplicateHider.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DuplicateHider.Converters
{
    public class PropertyIdToNameConverter : IMultiValueConverter
    {
        private IPlayniteAPI playniteAPI;
        public string PropertyName { get; set; }

        public PropertyIdToNameConverter()
        {
            this.playniteAPI = DuplicateHiderPlugin.Instance.PlayniteApi;
        }

        public PropertyIdToNameConverter(string propertyName)
        {
            this.playniteAPI = DuplicateHiderPlugin.Instance.PlayniteApi;
            this.PropertyName = propertyName;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return null;
            if (values[0]?.ToString() is string s && values[1] is PriorityProperty priorityProperty)
            {
                var game = new Game();
                var propertyName = priorityProperty.PropertyName;
                {
                    if (Guid.TryParse(s, out var id))
                    {
                        if (propertyName == nameof(Game.SourceId))
                        {
                            if (id == default)
                            {
                                return Constants.UNDEFINED_SOURCE;
                            }
                            else
                            {
                                return playniteAPI.Database.Sources.Get(id)?.Name;
                            }
                        }
                        else if (propertyName == nameof(Game.CompletionStatusId))
                        {
                            return playniteAPI.Database.CompletionStatuses.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.PluginId))
                        {
                            return playniteAPI.Addons.Plugins.OfType<LibraryPlugin>().FirstOrDefault(p => p.Id == id)?.Name ?? Constants.UNDEFINED_SOURCE;
                        }
                        else if (propertyName == nameof(Game.PlatformIds))
                        {
                            return playniteAPI.Database.Platforms.Get(id)?.Name ?? Constants.UNDEFINED_PLATFORM;
                        }
                        else if (propertyName == nameof(Game.GenreIds))
                        {
                            return playniteAPI.Database.Genres.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.PublisherIds) || propertyName == nameof(Game.DeveloperIds))
                        {
                            return playniteAPI.Database.Companies.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.FeatureIds))
                        {
                            return playniteAPI.Database.Features.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.CategoryIds))
                        {
                            return playniteAPI.Database.Categories.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.SeriesIds))
                        {
                            return playniteAPI.Database.Series.Get(id)?.Name;
                        }
                        else if (propertyName == nameof(Game.RegionIds))
                        {
                            return playniteAPI.Database.Regions.Get(id)?.Name;
                        }
                    }
                    if (bool.TryParse(s, out var state))
                    {
                        if (propertyName == nameof(Game.IsInstalled))
                        {
                            return state ? ResourceProvider.GetString("LOCGameIsGameInstalledTitle") : ResourceProvider.GetString("LOCGameIsUnInstalledTitle");
                        }
                        if (propertyName == nameof(Game.Favorite))
                        {
                            return state ? ResourceProvider.GetString("LOCGameFavoriteTitle") : ResourceProvider.GetString("LOCNone");
                        }
                    }
                }
            }

            return values[0].ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
