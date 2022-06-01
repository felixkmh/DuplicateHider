using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DuplicateHider.Converters
{

    public class GamePropertyToNameConverter : IValueConverter
    {
        public static readonly GamePropertyToNameConverter Instance = new GamePropertyToNameConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string propertyName)
            {
                if (propertyName == nameof(Game.Added)) return ResourceProvider.GetString("LOCDateAddedLabel");
                if (propertyName == nameof(Game.CategoryIds)) return ResourceProvider.GetString("LOCCategoriesLabel");
                if (propertyName == nameof(Game.CommunityScore)) return ResourceProvider.GetString("LOCCommunityScore");
                if (propertyName == nameof(Game.CompletionStatusId)) return ResourceProvider.GetString("LOCCompletionStatus");
                if (propertyName == nameof(Game.CriticScore)) return ResourceProvider.GetString("LOCCriticScore");
                if (propertyName == nameof(Game.DeveloperIds)) return ResourceProvider.GetString("LOCGameDevelopersTitle");
                if (propertyName == nameof(Game.Favorite)) return ResourceProvider.GetString("LOCGameFavoriteTitle");
                if (propertyName == nameof(Game.FeatureIds)) return ResourceProvider.GetString("LOCFeatureLabel");
                if (propertyName == nameof(Game.GenreIds)) return ResourceProvider.GetString("LOCGameGenresTitle");
                if (propertyName == nameof(Game.IsInstalled)) return ResourceProvider.GetString("LOCGameInstallationStatus");
                if (propertyName == nameof(Game.LastActivity)) return ResourceProvider.GetString("LOCGameLastActivityTitle");
                if (propertyName == nameof(Game.PlatformIds)) return ResourceProvider.GetString("LOCGamePlatformTitle");
                if (propertyName == nameof(Game.PlayCount)) return ResourceProvider.GetString("LOCPlayCountLabel");
                if (propertyName == nameof(Game.Playtime)) return ResourceProvider.GetString("LOCTimePlayed");
                if (propertyName == nameof(Game.PluginId)) return ResourceProvider.GetString("LOCLibraries");
                if (propertyName == nameof(Game.PublisherIds)) return ResourceProvider.GetString("LOCGamePublishersTitle");
                if (propertyName == nameof(Game.RegionIds)) return ResourceProvider.GetString("LOCRegionLabel");
                if (propertyName == nameof(Game.ReleaseDate)) return ResourceProvider.GetString("LOCGameReleaseDateTitle");
                if (propertyName == nameof(Game.SeriesIds)) return ResourceProvider.GetString("LOCSeriesLabel");
                if (propertyName == nameof(Game.SourceId)) return ResourceProvider.GetString("LOCSourceLabel");
                if (propertyName == nameof(Game.UserScore)) return ResourceProvider.GetString("LOCUserScore");
                if (propertyName == nameof(Game.Name)) return ResourceProvider.GetString("LOCGameNameTitle");
                if (propertyName == nameof(Game.SortingName)) return ResourceProvider.GetString("LOCGameSortingNameTitle");
                if (propertyName == nameof(Game.Version)) return ResourceProvider.GetString("LOCVersionLabel");
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
