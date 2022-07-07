using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DuplicateHider.Models;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DuplicateHider.ViewModels
{
    public class CopyFieldsViewModel : ObservableObject
    {
        private static DuplicateHider.Converters.DatabaseIdToNameConverter DatabaseIdToNameConverter = new DuplicateHider.Converters.DatabaseIdToNameConverter();

        protected IEnumerable<CopyFieldsModel> copyFields = Array.Empty<CopyFieldsModel>();
        public IEnumerable<CopyFieldsModel> CopyFields { get => copyFields; set { SetValue(ref copyFields, value); OnPropertyChanged(nameof(Sources)); } }

        protected EnabledFieldsModel enabledFields = DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields.Copy();
        public EnabledFieldsModel EnabledFields { get => enabledFields; set => SetValue(ref enabledFields, value); }

        public IEnumerable<Game> Sources => CopyFields.Select(cf => cf.SourceGame);

        protected bool saveAsDefault = false;
        public bool SaveAsDefault { get => saveAsDefault; set => SetValue(ref saveAsDefault, value); }

        public ICommand ApplyCommand { get; protected set; }
        public ICommand RevertCommand { get; protected set; }

        public class CheckableGuid
        {
            public string Name { get; set; }
            public Guid Guid { get; set; }
            public bool IsChecked { get; set; }
        }

        public CopyFieldsViewModel()
        {
            ApplyCommand = new RelayCommand(() =>
            {
                if (CopyFields.Count() > 0)
                {
                    if (DuplicateHiderPlugin.API.Dialogs.ShowMessage(
                        string.Format(ResourceProvider.GetString("LOC_DH_CopyFieldsWarning"), ResourceProvider.GetString("LOCOKLabel"), ResourceProvider.GetString("LOCCancelLabel")),
                        ResourceProvider.GetString("LOC_DH_Warning"), System.Windows.MessageBoxButton.OKCancel)
                    == System.Windows.MessageBoxResult.OK)
                    {
                        DuplicateHiderPlugin.API.Dialogs.ActivateGlobalProgress(args =>
                        {
                            int done = 0;
                            int total = CopyFields.Count();
                            args.ProgressMaxValue = total;
                            args.Text = string.Format("{0}/{1}", done, total);
                            args.CurrentProgressValue = done;
                            UpdateExclusionSets();
                            foreach (var cf in CopyFields)
                            {
                                if (args.CancelToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                cf.Apply(EnabledFields);

                                args.Text = string.Format("{0}/{1}", ++done, total);
                                args.CurrentProgressValue = done;
                            }
                            args.Text = ResourceProvider.GetString("LOC_DH_Updating");
                            DuplicateHiderPlugin.API.Database.Games.Update(CopyFields.SelectMany(cf => cf.TargetGames).Union(CopyFields.Select(cf => cf.SourceGame)));
                            if (SaveAsDefault) DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields = EnabledFields;
                        }, new GlobalProgressOptions(string.Format("{0}/{1}", 0, CopyFields.Count()), true) { IsIndeterminate = false });
                    }
                }
                GC.Collect();
            });
            RevertCommand = new RelayCommand(() =>
            {
                CopyFields.ForEach(cf => cf.Revert());
            });
        }

        private void UpdateExclusionSets()
        {
            EnabledFields.AgeRatingsExcluded = ExcludedAgeRatings.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.CategoriesExcluded = ExcludedCategories.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.DevelopersExcluded = ExcludedDevelopers.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.FeaturesExcluded = ExcludedFeatures.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.GenresExcluded = ExcludedGenres.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.PlatformsExcluded = ExcludedPlatforms.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.PublishersExcluded = ExcludedPublishers.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.RegionsExcluded = ExcludedRegions.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.SeriesExcluded = ExcludedSeries.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
            EnabledFields.TagsExcluded = ExcludedTags.Where(p => p.IsChecked).Select(p => p.Guid).ToHashSet();
        }

            public CopyFieldsViewModel(Game source, IEnumerable<Game> targets) : this()
        {
            copyFields = new CopyFieldsModel[] { new CopyFieldsModel(source, targets) };
        }

        public CopyFieldsViewModel(IEnumerable<CopyFieldsModel> copyFields) : this()
        {
            this.copyFields = copyFields;
        }

        private ObservableCollection<CheckableGuid> excludedPlatforms = null;
        public ObservableCollection<CheckableGuid> ExcludedPlatforms
        {
            get
            {
                if (excludedPlatforms == null)
                {
                    excludedPlatforms = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Platforms
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.PlatformsExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedPlatforms;
            }
        }

        private ObservableCollection<CheckableGuid> excludedGenres = null;
        public ObservableCollection<CheckableGuid> ExcludedGenres
        {
            get
            {
                if (excludedGenres == null)
                {
                    excludedGenres = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Genres
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.GenresExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedGenres;
            }
        }

        private ObservableCollection<CheckableGuid> excludedDevelopers = null;
        public ObservableCollection<CheckableGuid> ExcludedDevelopers
        {
            get
            {
                if (excludedDevelopers == null)
                {
                    excludedDevelopers = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Companies
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.DevelopersExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedDevelopers;
            }
        }

        private ObservableCollection<CheckableGuid> excludedPublishers = null;
        public ObservableCollection<CheckableGuid> ExcludedPublishers
        {
            get
            {
                if (excludedPublishers == null)
                {
                    excludedPublishers = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Companies
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.PublishersExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedPublishers;
            }
        }

        private ObservableCollection<CheckableGuid> excludedCategories = null;
        public ObservableCollection<CheckableGuid> ExcludedCategories
        {
            get
            {
                if (excludedCategories == null)
                {
                    excludedCategories = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Categories
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.CategoriesExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedCategories;
            }
        }

        private ObservableCollection<CheckableGuid> excludedFeatures = null;
        public ObservableCollection<CheckableGuid> ExcludedFeatures
        {
            get
            {
                if (excludedFeatures == null)
                {
                    excludedFeatures = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Features
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.FeaturesExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedFeatures;
            }
        }

        private ObservableCollection<CheckableGuid> excludedTags = null;
        public ObservableCollection<CheckableGuid> ExcludedTags
        {
            get
            {
                if (excludedTags == null)
                {
                    excludedTags = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Tags
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.TagsExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedTags;
            }
        }

        private ObservableCollection<CheckableGuid> excludedSeries = null;
        public ObservableCollection<CheckableGuid> ExcludedSeries
        {
            get
            {
                if (excludedSeries == null)
                {
                    excludedSeries = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Series
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.SeriesExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedSeries;
            }
        }

        private ObservableCollection<CheckableGuid> excludedAgeRatings = null;
        public ObservableCollection<CheckableGuid> ExcludedAgeRatings
        {
            get
            {
                if (excludedAgeRatings == null)
                {
                    excludedAgeRatings = DuplicateHiderPlugin.Instance.PlayniteApi.Database.AgeRatings
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.AgeRatingsExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedAgeRatings;
            }
        }

        private ObservableCollection<CheckableGuid> excludedRegions = null;
        public ObservableCollection<CheckableGuid> ExcludedRegions
        {
            get
            {
                if (excludedRegions == null)
                {
                    excludedRegions = DuplicateHiderPlugin.Instance.PlayniteApi.Database.Regions
                        .Select(p => new CheckableGuid { Guid = p.Id, IsChecked = EnabledFields.RegionsExcluded.Contains(p.Id), Name = DatabaseIdToNameConverter.Convert(p.Id, typeof(string), null, null) as string })
                        .OrderByDescending(p => p.IsChecked)
                        .ThenBy(p => p.Name)
                        .ToObservable();
                }
                return excludedRegions;
            }
        }
    }
}
