using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DuplicateHider.Models
{
    public class CopyFieldsModel : ObservableObject
    {
        protected Game sourceGame = null;
        public Game SourceGame { get => sourceGame; set => SetValue(ref sourceGame, value); }

        protected IEnumerable<Game> targetGames = Array.Empty<Game>();
        public IEnumerable<Game> TargetGames { get => targetGames; set => SetValue(ref targetGames, value); }

        protected List<Game> backup = new List<Game>();

        public CopyFieldsModel(Game source, IEnumerable<Game> targets)
        {
            sourceGame = source;
            targetGames = targets;
            foreach(var game in targets)
            {
                // backup.Add(game.Copy());
            }
        }

        public void Apply(EnabledFieldsModel enabledFields)
        {
            var targets = TargetGames;
            var dhTagIds = new[] {
                DuplicateHiderPlugin.Instance.settings.HiddenTagId,
                DuplicateHiderPlugin.Instance.settings.RevealedTagId,
                DuplicateHiderPlugin.Instance.settings.HighPrioTagId,
                DuplicateHiderPlugin.Instance.settings.LowPrioTagId
            };
            if (enabledFields.Name) targets.ForEach(g => g.Name = SourceGame.Name);
            if (enabledFields.SortingName) targets.ForEach(g => g.SortingName = SourceGame.SortingName);
            if (enabledFields.Platforms) targets.ForEach(g => g.PlatformIds = SourceGame.PlatformIds);
            if (enabledFields.Genres) targets.ForEach(g => g.GenreIds = SourceGame.GenreIds);
            if (enabledFields.Developers) targets.ForEach(g => g.DeveloperIds = SourceGame.DeveloperIds);
            if (enabledFields.Publishers) targets.ForEach(g => g.PublisherIds = SourceGame.PublisherIds);
            if (enabledFields.Categories) targets.ForEach(g => g.CategoryIds = SourceGame.CategoryIds);
            if (enabledFields.Features) targets.ForEach(g => g.FeatureIds = SourceGame.FeatureIds);
            if (enabledFields.Tags) targets.ForEach(g => g.TagIds = SourceGame.TagIds.Where(t => !dhTagIds.Contains(t)).Concat(g.TagIds.Where(id => dhTagIds.Contains(id))).Distinct().ToList());
            if (enabledFields.Description) targets.ForEach(g => g.Description = SourceGame.Description);
            if (enabledFields.ReleaseDate) targets.ForEach(g => g.ReleaseDate = SourceGame.ReleaseDate);
            if (enabledFields.Series) targets.ForEach(g => g.SeriesIds = SourceGame.SeriesIds);
            if (enabledFields.AgeRatings) targets.ForEach(g => g.AgeRatingIds = SourceGame.AgeRatingIds);
            if (enabledFields.Regions) targets.ForEach(g => g.RegionIds = SourceGame.RegionIds);
            if (enabledFields.Version) targets.ForEach(g => g.Version = SourceGame.Version);
            if (enabledFields.UserScore) targets.ForEach(g => g.UserScore = SourceGame.UserScore);
            if (enabledFields.CriticsScore) targets.ForEach(g => g.CriticScore = SourceGame.CriticScore);
            if (enabledFields.CommunityScore) targets.ForEach(g => g.CommunityScore = SourceGame.CommunityScore);
            if (enabledFields.Links) targets.ForEach(g => g.Links = SourceGame.Links);

            { // copy background image
                if (enabledFields.BackgroundImage && SourceGame.BackgroundImage is string imagePath && !string.IsNullOrWhiteSpace(imagePath))
                {
                    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                     || (Path.IsPathRooted(imagePath) && File.Exists(imagePath)))
                    {
                        foreach (var target in targets)
                        {
                            target.BackgroundImage = imagePath;
                        }
                    }
                    else if (DuplicateHiderPlugin.API.Database.GetFullFilePath(imagePath) is string fullPath && File.Exists(fullPath))
                    {
                        foreach (var target in targets)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(target.BackgroundImage)) DuplicateHiderPlugin.API.Database.RemoveFile(target.BackgroundImage);
                                var databasePath = DuplicateHiderPlugin.API.Database.AddFile(fullPath, target.Id);
                                target.BackgroundImage = databasePath;
                            }
                            catch (Exception) {}
                        }
                    }
                }
            }
            { // copy icon image
                if (enabledFields.Icon && SourceGame.Icon is string imagePath && !string.IsNullOrWhiteSpace(imagePath))
                {
                    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                     || (Path.IsPathRooted(imagePath) && File.Exists(imagePath)))
                    {
                        foreach (var target in targets)
                        {
                            target.Icon = imagePath;
                        }
                    }
                    else if (DuplicateHiderPlugin.API.Database.GetFullFilePath(imagePath) is string fullPath && File.Exists(fullPath))
                    {
                        foreach (var target in targets)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(target.Icon)) DuplicateHiderPlugin.API.Database.RemoveFile(target.Icon);
                                var databasePath = DuplicateHiderPlugin.API.Database.AddFile(fullPath, target.Id);
                                target.Icon = databasePath;
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            { // copy cover image
                if (enabledFields.CoverImage && SourceGame.CoverImage is string imagePath && !string.IsNullOrWhiteSpace(imagePath))
                {
                    if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                     || (Path.IsPathRooted(imagePath) && File.Exists(imagePath)))
                    {
                        foreach (var target in targets)
                        {
                            target.CoverImage = imagePath;
                        }
                    }
                    else if (DuplicateHiderPlugin.API.Database.GetFullFilePath(imagePath) is string fullPath && File.Exists(fullPath))
                    {
                        foreach (var target in targets)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(target.CoverImage)) DuplicateHiderPlugin.API.Database.RemoveFile(target.CoverImage);
                                var databasePath = DuplicateHiderPlugin.API.Database.AddFile(fullPath, target.Id);
                                target.CoverImage = databasePath;
                            }
                            catch (Exception) {
                            }
                        }
                    }
                }
            }

            // DuplicateHiderPlugin.API.Database.Games.Update(targets);
        }

        public void Revert()
        {
            // DuplicateHiderPlugin.API.Database.Games.Update(backup);
        }
    }
}
