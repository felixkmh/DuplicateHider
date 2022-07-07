using Playnite.SDK.Models;
using System;
using System.Collections;
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

        public void UnionizeGuidList(string propertyName, Game source, IEnumerable<Game> targets, IEnumerable<Guid> excluded = null)
        {
            excluded = excluded ?? Enumerable.Empty<Guid>();

            var property = typeof(Game).GetProperty(propertyName);
            if (property != null && property.CanRead && property.CanWrite && property.PropertyType == typeof(List<Guid>))
            {
                var union = new HashSet<Guid>();
                var sourceValues = property.GetValue(source) as List<Guid>;
                if (sourceValues != null)
                {
                    foreach(var sourceValue in sourceValues)
                    {
                        if (!excluded.Contains(sourceValue))
                        {
                            union.Add(sourceValue);
                        }
                    }
                }
                foreach(var target in targetGames)
                {
                    var targetValues = property.GetValue(target) as List<Guid>;
                    if (targetValues != null)
                    {
                        foreach(var targetValue in targetValues)
                        {
                            if (!excluded.Contains(targetValue))
                            {
                                union.Add(targetValue);
                            }
                        }
                    }
                }
                if (sourceValues == null)
                {
                    sourceValues = new List<Guid>();
                }
                property.SetValue(source, union.Union(sourceValues.Intersect(excluded)).ToList());
                foreach(var target in targets)
                {
                    var newTargetValues = union;
                    var targetValues = property.GetValue(target) as List<Guid>;
                    if (targetValues != null)
                    {
                        newTargetValues = union.Union(targetValues.Intersect(excluded)).ToHashSet();
                    }
                    property.SetValue(target, newTargetValues.ToList());
                }
            }
        }

        public void Apply(EnabledFieldsModel enabledFields)
        {
            try
            {
                var targets = TargetGames;
                var dhTagIds = new HashSet<Guid> {
                DuplicateHiderPlugin.Instance.settings.HiddenTagId,
                DuplicateHiderPlugin.Instance.settings.RevealedTagId,
                DuplicateHiderPlugin.Instance.settings.HighPrioTagId,
                DuplicateHiderPlugin.Instance.settings.LowPrioTagId
            };
                if (enabledFields.Name) targets.ForEach(g => g.Name = SourceGame.Name);
                if (enabledFields.SortingName) targets.ForEach(g => g.SortingName = SourceGame.SortingName);

                if (enabledFields.Platforms)
                    if (enabledFields.PlatformsUnion)
                    {
                        UnionizeGuidList(nameof(Game.PlatformIds), SourceGame, TargetGames, enabledFields.PlatformsExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.PlatformIds = SourceGame.PlatformIds?.Where(id => !enabledFields.PlatformsExcluded.Contains(id)).ToList() ?? g.PlatformIds);
                    }


                if (enabledFields.Genres)
                    if (enabledFields.GenresUnion)
                    {
                        UnionizeGuidList(nameof(Game.GenreIds), SourceGame, TargetGames, enabledFields.GenresExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.GenreIds = SourceGame.GenreIds?.Where(id => !enabledFields.GenresExcluded.Contains(id)).ToList() ?? g.GenreIds);
                    }

                if (enabledFields.Developers)
                    if (enabledFields.DevelopersUnion)
                    {
                        UnionizeGuidList(nameof(Game.DeveloperIds), SourceGame, TargetGames, enabledFields.DevelopersExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.DeveloperIds = SourceGame.DeveloperIds?.Where(id => !enabledFields.DevelopersExcluded.Contains(id)).ToList() ?? g.DeveloperIds);
                    }

                if (enabledFields.Publishers)
                    if (enabledFields.PublishersUnion)
                    {
                        UnionizeGuidList(nameof(Game.PublisherIds), SourceGame, TargetGames, enabledFields.PublishersExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.PublisherIds = SourceGame.PublisherIds?.Where(id => !enabledFields.PublishersExcluded.Contains(id)).ToList() ?? g.PublisherIds);
                    }


                if (enabledFields.Categories)
                    if (enabledFields.CategoriesUnion)
                    {
                        UnionizeGuidList(nameof(Game.CategoryIds), SourceGame, TargetGames, enabledFields.CategoriesExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.CategoryIds = SourceGame.CategoryIds?.Where(id => !enabledFields.CategoriesExcluded.Contains(id)).ToList() ?? g.CategoryIds);
                    }

                if (enabledFields.Features)
                    if (enabledFields.FeaturesUnion)
                    {
                        UnionizeGuidList(nameof(Game.FeatureIds), SourceGame, TargetGames, enabledFields.FeaturesExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.FeatureIds = SourceGame.FeatureIds?.Where(id => !enabledFields.FeaturesExcluded.Contains(id)).ToList() ?? g.FeatureIds);
                    }

                if (enabledFields.Tags)
                    if (enabledFields.TagsUnion)
                    {
                        UnionizeGuidList(nameof(Game.TagIds), SourceGame, TargetGames, dhTagIds);
                    }
                    else
                    {
                        targets.ForEach(g => g.TagIds = SourceGame.TagIds?.Where(t => (!dhTagIds.Contains(t) && !enabledFields.TagsExcluded.Contains(t))).Concat(g.TagIds.Where(id => dhTagIds.Contains(id))).Distinct().ToList() ?? g.TagIds);
                    }

                if (enabledFields.Series)
                    if (enabledFields.SeriesUnion)
                    {
                        UnionizeGuidList(nameof(Game.SeriesIds), SourceGame, TargetGames, enabledFields.SeriesExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.SeriesIds = SourceGame.SeriesIds?.Where(id => !enabledFields.SeriesExcluded.Contains(id)).ToList() ?? g.SeriesIds);
                    }

                if (enabledFields.AgeRatings)
                    if (enabledFields.AgeRatingsUnion)
                    {
                        UnionizeGuidList(nameof(Game.AgeRatingIds), SourceGame, TargetGames, enabledFields.AgeRatingsExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.AgeRatingIds = SourceGame.AgeRatingIds?.Where(id => !enabledFields.AgeRatingsExcluded.Contains(id)).ToList() ?? g.AgeRatingIds);
                    }

                if (enabledFields.Regions)
                    if (enabledFields.RegionsUnion)
                    {
                        UnionizeGuidList(nameof(Game.RegionIds), SourceGame, TargetGames, enabledFields.RegionsExcluded);
                    }
                    else
                    {
                        targets.ForEach(g => g.RegionIds = SourceGame.RegionIds?.Where(id => !enabledFields.RegionsExcluded.Contains(id)).ToList() ?? g.RegionIds);
                    }

                if (enabledFields.Links)
                    if (enabledFields.LinksUnion)
                    {
                        List<Link> linkUnion = new List<Link>();
                        sourceGame?.Links?.ForEach(l => linkUnion.Add(l));
                        foreach (var target in TargetGames)
                        {
                            target?.Links?.ForEach(l =>
                            {
                                if (!linkUnion.Any(existing => existing.Name == l.Name && existing.Url == l.Url))
                                {
                                    linkUnion.Add(l);
                                }
                            });
                        }
                        var obsersvableLinks = linkUnion.ToObservable();
                        sourceGame.Links = obsersvableLinks;
                        foreach (var target in TargetGames)
                        {
                            target.Links = obsersvableLinks;
                        }
                    }
                    else
                    {
                        targets.ForEach(g => g.Links = SourceGame.Links ?? g.Links);
                    }

                if (enabledFields.Description) targets.ForEach(g => g.Description = SourceGame.Description ?? g.Description);
                if (enabledFields.ReleaseDate) targets.ForEach(g => g.ReleaseDate = SourceGame.ReleaseDate ?? g.ReleaseDate);
                if (enabledFields.Version) targets.ForEach(g => g.Version = SourceGame.Version ?? g.Version);
                if (enabledFields.UserScore) targets.ForEach(g => g.UserScore = SourceGame.UserScore ?? g.UserScore);
                if (enabledFields.CriticsScore) targets.ForEach(g => g.CriticScore = SourceGame.CriticScore ?? g.CriticScore);
                if (enabledFields.CommunityScore) targets.ForEach(g => g.CommunityScore = SourceGame.CommunityScore ?? g.CommunityScore);
                if (enabledFields.CompletionStatus) targets.ForEach(g => g.CompletionStatusId = SourceGame.CompletionStatusId);

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
                                catch (Exception) { }
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
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                }

                // ExtraMetaData
                var emtPath = Path.Combine(DuplicateHiderPlugin.API.Paths.ConfigurationPath, "ExtraMetadata", "games");
                var sourceEmt = Path.Combine(emtPath, SourceGame.Id.ToString().ToLower());
                if (Directory.Exists(emtPath) && Directory.Exists(sourceEmt))
                {
                    foreach (var target in targets)
                    {
                        var targetEmt = Path.Combine(emtPath, target.Id.ToString().ToLower());
                        if (!Directory.Exists(targetEmt))
                        {
                            Directory.CreateDirectory(targetEmt);
                        }
                        var sourceLogo = Directory.GetFiles(sourceEmt, "Logo.*").FirstOrDefault();
                        if (enabledFields.Logo && !string.IsNullOrEmpty(sourceLogo))
                        {
                            try
                            {
                                var sourceFileName = Path.GetFileName(sourceLogo);
                                var targetLogo = Path.Combine(targetEmt, sourceFileName);
                                File.Copy(sourceLogo, targetLogo, true);
                            }
                            catch (Exception)
                            { }
                        }
                        var sourceTrailer = Directory.GetFiles(sourceEmt, "VideoTrailer.*").FirstOrDefault();
                        if (enabledFields.Trailer && !string.IsNullOrEmpty(sourceTrailer))
                        {
                            try
                            {
                                var sourceFileName = Path.GetFileName(sourceTrailer);
                                var targetTrailer = Path.Combine(targetEmt, sourceFileName);
                                File.Copy(sourceTrailer, targetTrailer, true);
                            }
                            catch (Exception)
                            { }
                        }
                        var sourceMicroTrailer = Directory.GetFiles(sourceEmt, "VideoMicrotrailer.*").FirstOrDefault();
                        if (enabledFields.MicroTrailer && !string.IsNullOrEmpty(sourceMicroTrailer))
                        {
                            try
                            {
                                var sourceFileName = Path.GetFileName(sourceMicroTrailer);
                                var targetMicroTrailer = Path.Combine(targetEmt, sourceFileName);
                                File.Copy(sourceTrailer, targetMicroTrailer, true);
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }

                // DuplicateHiderPlugin.API.Database.Games.Update(targets);
            }
            catch (Exception ex)
            {
                var fieldsString = Newtonsoft.Json.JsonConvert.SerializeObject(enabledFields, Newtonsoft.Json.Formatting.Indented);
                DuplicateHiderPlugin.logger.Error(ex, $"Failed to copy fields from {SourceGame?.Name}.\nEnabled Fields:\n{fieldsString}");
                throw;
            }
        }

        public void Revert()
        {
            // DuplicateHiderPlugin.API.Database.Games.Update(backup);
        }
    }
}
