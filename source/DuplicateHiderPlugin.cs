using DuplicateHider.Cache;
using DuplicateHider.Controls;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using QuickSearch.SearchItems;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static DuplicateHider.DuplicateHiderPlugin.Visibility;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DuplicateHider.Data;
using System.Windows.Input;
using DuplicateHider.Models;
using DuplicateHider.ViewModels;
using DuplicateHider.Views;
using StartPage.SDK;
using System.Collections.ObjectModel;
using System.Diagnostics;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider
{
    public class DuplicateHiderPlugin : GenericPlugin, StartPage.SDK.IStartPageExtension
    {
        private GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new GongSolutions.Wpf.DragDrop.DefaultDragHandler();

        public event EventHandler<IEnumerable<Guid>> GroupUpdated;
        internal struct GameSelectedArgs { public Guid? oldId; public Guid? newId; }
        internal event EventHandler<GameSelectedArgs> GameSelected;

        internal static readonly ILogger logger = LogManager.GetLogger();

        public DuplicateHiderSettings Settings => settings;

        internal DuplicateHiderSettings settings { get; private set; }
        internal static IPlayniteAPI API { get; private set; } = null;

        public static DuplicateHiderPlugin Instance { get; private set; }

        public override Guid Id { get; } = Guid.Parse("382f8003-8ed0-4e47-ae93-05b43c9c6c32");

        internal Dictionary<string, List<Guid>> Index { get; set; } = new Dictionary<string, List<Guid>>();
        internal Dictionary<Guid, List<Game>> GuidToCopies { get; set; } = new Dictionary<Guid, List<Game>>();

        internal static readonly IconCache SourceIconCache = new IconCache();

        public DuplicateHiderSettingsView SettingsView { get; private set; }

        private FileSystemWatcher iconWatcher = null;

        public Guid? CurrentlySelected { get; private set; } = null;

        internal static ICommand editGamesCommand;

        public DuplicateHiderPlugin(IPlayniteAPI api) : base(api)
        {
            API = api;
            Instance = this;
            settings = new DuplicateHiderSettings(this);
            Properties = new GenericPluginProperties { HasSettings = true };
            var elements = new HashSet<string>() { "SourceSelector" };
            for (int i = 1; i < Constants.NUMBEROFSOURCESELECTORS; ++i)
            {
                bool foundStyle = false;
                bool validStackStyle = true;
                bool validButtonStyle = true;
                if (PlayniteApi.Resources.GetResource($"DuplicateHider_IconStackPanelStyle{i}") is Style stackStyle)
                {
                    foundStyle = true;
                    validStackStyle = stackStyle.TargetType == typeof(StackPanel);
                }
                if (PlayniteApi.Resources.GetResource($"DuplicateHider_IconContentControlStyle{i}") is Style buttonStyle)
                {
                    foundStyle = true;
                    validButtonStyle = buttonStyle.TargetType == typeof(ContentControl);
                }
                if (foundStyle && validButtonStyle && validStackStyle)
                {
                    var name = "SourceSelector".Suffix(i);
                    elements.Add(name);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Added {name} as Custom Element.");
                    logger.Debug($"Added {name} as Custom Element.");
#endif
                }
            }

            for (int i = 0; i < Constants.NUMBEROFSOURCESELECTORS; ++i)
            {
                string name = "ContentControl".Suffix(i);
                elements.Add(name);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Added {name} as Custom Element.");
                logger.Debug($"Added {name} as Custom Element.");
#endif
            }

            AddCustomElementSupport(new AddCustomElementSupportArgs()
            {
                ElementList = elements.ToList(),
                SourceName = "DuplicateHider"
            });

            AddSettingsSupport(new AddSettingsSupportArgs { SettingsRoot = "Settings", SourceName = "DuplicateHider" });
        }

        internal void UpdateGuidToCopiesDict()
        {
            GuidToCopies.Clear();
            foreach (var pair in Index)
            {
                var copies = pair.Value.Select(id => PlayniteApi.Database.Games.Get(id)).Where(g => g is Game).ToList();
                foreach (var id in pair.Value)
                {
                    GuidToCopies[id] = copies;
                }
            }
        }

        internal IEnumerable<Guid> UpdateGuidToCopiesDict(IEnumerable<Guid> updatedIds)
        {
            var requireUpdate = new HashSet<Guid>();
            if (updatedIds?.Any() ?? false)
            {
                var nameFilter = GetNameFilter();
                foreach (var id in updatedIds)
                {
                    if (GuidToCopies.TryGetValue(id, out var oldList))
                    {
                        oldList.ForEach(g => requireUpdate.Add(g.Id));
                    }
                    GuidToCopies.Remove(id);
                    var game = PlayniteApi.Database.Games.Get(id);
                    if (game is Game)
                    {
                        string name = GetFilteredName(game, nameFilter);
                        if (Index.TryGetValue(name, out var copies))
                        {
                            var games = copies
                                .Select(c => PlayniteApi.Database.Games.Get(c))
                                .OfType<Game>()
                                .ToList();
                            foreach(var copy in games)
                            {
                                GuidToCopies[copy.Id] = games;
                                requireUpdate.Add(copy.Id);
                            }
                        }
                    }
                }
            }
            return requireUpdate;
        }
#if DEBUG
        static int GeneratedElements = 0;
#endif
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (settings.EnableUiIntegration)
            {
                if (args.Name == "SourceSelector")
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Generated SourceSelector:" + (++GeneratedElements));
#endif
                    return new SourceSelector(0, Orientation.Horizontal);
                } else
                if (args.Name.StartsWith("SourceSelector"))
                {
                    if (int.TryParse(args.Name.Substring(14), out int n))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Generated SourceSelector:" + (++GeneratedElements));
#endif
                        return new SourceSelector(n, Orientation.Horizontal);
                    }


                } else if (args.Name.StartsWith("ContentControl"))
                {
                    if (!int.TryParse(args.Name.Substring(14), out int n))
                    {
                        n = 0;
                    }
                    var wrapper = new DHWrapper();
                    if (ResourceProvider.GetResource("DuplicateHider_MaxNumberOfIcons".Suffix(n)) != null)
                    {
                        wrapper.DH_ContentControl.SetResourceReference(DHContentControl.MaxNumberOfIconsCCProperty, "DuplicateHider_MaxNumberOfIcons".Suffix(n));
                    }
                    return wrapper;
                }
            }
            return null;
        }

#region Events       
        public override async void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            //PlayniteApi.Database.Games.ItemUpdated += (sender, itemUpdatedArgs) =>
            //{

            //    PlayniteApi.Dialogs.ShowMessage($"ItemUpdateEvent triggered with {itemUpdatedArgs.UpdatedItems.Count} updates.");

            //};

            if (settings.PriorityProperties == null)
            {
                var sourceIds = settings.Priorities
                    .Select(name =>
                    {
                        var source = PlayniteApi.Database.Sources.FirstOrDefault(s => s.Name == name);
                        if (source == null && name == Constants.UNDEFINED_SOURCE)
                        {
                            source = Constants.DEFAULT_SOURCE;
                        }
                        return source;
                    })
                    .OfType<GameSource>()
                    .Distinct()
                    .Select(s => s.Id.ToString());

                settings.PriorityProperties = new ObservableCollection<PriorityProperty>();

                settings.PriorityProperties.Add(new PriorityProperty(nameof(Game.IsInstalled), PlayniteApi));
                settings.PriorityProperties.Add(new PriorityProperty { PropertyName = nameof(Game.SourceId), PriorityList = sourceIds.ToObservable() });
                if (settings.PreferNewerGame)
                {
                    settings.PriorityProperties.Add(new PriorityProperty { PropertyName = nameof(Game.ReleaseDate), Direction = ListSortDirection.Descending });
                }
                settings.PriorityProperties.Add(new PriorityProperty { PropertyName = nameof(Game.Added), Direction = ListSortDirection.Descending });
            }

            foreach(var prio in settings.PriorityProperties)
            {
                if (!prio.IsList)
                {
                    prio.PriorityList.Clear();
                }
            }

            if (UiIntegration.FindVisualChildren(Application.Current.MainWindow, "PART_ListGames").FirstOrDefault() is FrameworkElement gameList)
            {
                var bindings = gameList.InputBindings;
                foreach (var binding in bindings.OfType<KeyBinding>())
                {
                    if (binding.Key == Key.F3)
                    {
                        editGamesCommand = binding.Command;
                    }
                }
            }



            // Create or set tags
            LocalizeTags();

            // Create icon folder
            if (!Directory.Exists(GetUserIconFolderPath()))
            {
                Directory.CreateDirectory(GetUserIconFolderPath());
            }

            iconWatcher = new FileSystemWatcher(GetUserIconFolderPath())
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            if (iconWatcher != null)
            {
                iconWatcher.Renamed += IconWatcher_Changed;
                iconWatcher.Created += IconWatcher_Changed;
                iconWatcher.Deleted += IconWatcher_Changed;
                iconWatcher.Changed += IconWatcher_Changed;
                iconWatcher.EnableRaisingEvents = true;
            }

            // Clean orphaned entries from Priorities list
            for (int i = settings.Priorities.Count - 1; i >= 0; --i)
            {
                var prio = settings.Priorities[i];
                if (prio == Constants.UNDEFINED_SOURCE)
                {
                    continue;
                }

                if (!PlayniteApi.Database.Sources.TryFind(s => s.Name == prio, out var source))
                {
                    settings.Priorities.RemoveAt(i);
                }
            }
            // Add new sources not yet contained in the Priorities list
            foreach (var source in PlayniteApi.Database.Sources)
            {
                settings.Priorities.Add(source.Name);
            }

            await UpdateIndexAsync();

            GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
            if (settings.UpdateAutomatically)
            {
                var toUpdate = SetDuplicateState(Hidden);
                if (toUpdate.Count > 0)
                {
                    PlayniteApi.Database.Games.Update(toUpdate);
                }
            }

            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            settings.OnSettingsChanged += Settings_OnSettingsChanged;


            // SourceSelector statics
            SourceIconCache.UserIconFolderPaths.Add(GetUserIconFolderPath());

            if (!settings.SupressThemeIconNotification && !settings.EnableThemeIcons)
            {
                var foundThemIcons = PlayniteApi.Database.Sources.Concat(new[] { Constants.DEFAULT_SOURCE }).Where(
                    s => PlayniteApi.Resources.GetResource($"DuplicateHider_{s.Name}_Icon") is BitmapImage img
                ).Select(s => s.Name);
                if (foundThemIcons.Count() > 0)
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "DHFOUNDTHEMEICONS", $"DuplicateHider:\nFound Icons in current Theme, but \"Theme Icon\" option is disabled.\nFound icons for " +
                            $"{string.Join(", ", foundThemIcons)}. " +
                            $"{(settings.PreferUserIcons ? "\nMight be overriden by user icons because of \"Prefer User Icons\" option." : "")}" +
                            $"\nClick here for a preview.",
                        NotificationType.Info, () =>
                        {
                            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                            {
                                ShowMinimizeButton = false,
                                ShowCloseButton = true,
                                ShowMaximizeButton = false
                            });

                            window.Height = 400;
                            window.Width = 400;
                            window.Title = "Theme Icon Preview";

                            window.Content = new Windows.IconPreview();

                            var iconData = new List<Windows.PreviewData>();
                            foreach (var source in PlayniteApi.Database.Sources.Concat(new[] { Constants.DEFAULT_SOURCE }))
                            {
                                if (PlayniteApi.Resources.GetResource($"DuplicateHider_{source.Name}_Icon") is BitmapImage img)
                                {
                                    iconData.Add(new Windows.PreviewData() { SourceIcon = img, SourceName = source.Name });
                                }
                            }
                            window.DataContext = iconData;
                            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            window.ShowDialog();
                        })
                    );
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "DHENABLETHEMEICONS", "Click here to enable Theme Icons.", NotificationType.Info, () =>
                        {
                            var oldSettings = settings.Copy();
                            settings.EnableThemeIcons = true;
                            Settings_OnSettingsChanged(oldSettings, settings);
                            PlayniteApi.Notifications.Remove("DHFOUNDTHEMEICONS");
                            PlayniteApi.Notifications.Remove("DHSUPRESSNOTIFICATION");
                        })
                    );
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "DHSUPRESSNOTIFICATION", "Click here to suppress this notification in the future.", NotificationType.Info, () =>
                        {
                            var oldSettings = settings.Copy();
                            settings.SupressThemeIconNotification = true;
                            Settings_OnSettingsChanged(oldSettings, settings);
                        })
                    );
                }
            }

            UpdateGuidToCopiesDict();

            // QuickSearch support
            try
            {
                QuickSearch.QuickSearchSDK.AddCommand(new DuplicateHiderItem()
                {
                    Actions = new List<ISearchAction<string>>()
                    {
                    new QuickSearch.SearchItems.CommandAction() {Name = "Hide", Action = () =>
                    {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var hidden = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(hidden);
                        PlayniteApi.Dialogs.ShowMessage($"{hidden.Where(g => g.Hidden).Count()} games have been hidden.", "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                },
                new QuickSearch.SearchItems.CommandAction() {Name = "Reveal", Action = () =>
                    {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Visible);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Dialogs.ShowMessage($"{revealed.Where(g => !g.Hidden).Count()} games have been revealed.", "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                }
            }
                });


            }
            catch (Exception)
            {

            }
        }

        private void LocalizeTags()
        {
            if (PlayniteApi.Database.Tags.Get(settings.HiddenTagId) is Tag hiddenTag)
            {
                hiddenTag.Name = ResourceProvider.GetString("LOC_DH_HiddenTag");
            }
            if (PlayniteApi.Database.Tags.Get(settings.RevealedTagId) is Tag revealedTag)
            {
                revealedTag.Name = ResourceProvider.GetString("LOC_DH_RevealedTag");
            }
            if (PlayniteApi.Database.Tags.Get(settings.LowPrioTagId) is Tag lowTag)
            {
                lowTag.Name = ResourceProvider.GetString("LOC_DH_LowPrioTag");
            }
            if (PlayniteApi.Database.Tags.Get(settings.HighPrioTagId) is Tag highTag)
            {
                highTag.Name = ResourceProvider.GetString("LOC_DH_HighPrioTag");
            }
        }

        private class DuplicateHiderItem : ISearchItem<string>
        {
            public IList<QuickSearch.SearchItems.ISearchKey<string>> Keys => new List<QuickSearch.SearchItems.ISearchKey<string>>
            {
                new QuickSearch.SearchItems.CommandItemKey() { Key = TopLeft, Weight = 1},
                new QuickSearch.SearchItems.CommandItemKey() { Key = BottomLeft, Weight = 1}
            };

            public IList<QuickSearch.SearchItems.ISearchAction<string>> Actions { get; set; }

            public QuickSearch.SearchItems.ScoreMode ScoreMode => QuickSearch.SearchItems.ScoreMode.WeightedMaxScore;

            public Uri Icon => null;

            public string TopLeft => "Duplicates";

            public string TopRight => null;

            public string BottomLeft => "Hide or reveal duplicate copies";

            public string BottomCenter => null;

            public string BottomRight => Instance.Index.Values.Sum(v => v.Count() - 1).ToString() + " duplicates found";

            public char? IconChar => '';

            public FrameworkElement DetailsView => null;
        }

        private void IconWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name is string)
            {
                if (
                       e.Name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
                    || e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    SourceIconCache.Clear();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                    });
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Name={e.Name}, Type={e.ChangeType}");
#endif
                }
            }
        }

        public string GetUserIconFolderPath()
        {
            string path = Path.Combine(
                                    GetPluginUserDataPath(),
                                    "source_icons"
                                );
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public void SelectGame(Guid? gameId)
        {
            if (gameId is Guid id && PlayniteApi.Database.Games.Get(id) is Game)
            {
                PlayniteApi.MainView.SelectGame(id);
            }
            if (gameId != CurrentlySelected)
            {
                var temp = CurrentlySelected;
                CurrentlySelected = gameId;
                GameSelected?.Invoke(this, new GameSelectedArgs() { oldId = temp, newId = gameId });
            }
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            var oldId = CurrentlySelected;
            var newId = args.NewValue?.FirstOrDefault()?.Id;
            if (oldId != newId)
            {
                CurrentlySelected = newId;
                GameSelected?.Invoke(this, new GameSelectedArgs() { oldId = oldId, newId = newId });
            }
            // GroupUpdated?.Invoke(this, args.OldValue.Select(g => g.Id).Concat(args.NewValue.Select(g => g.Id)).Distinct());
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            if (iconWatcher != null)
            {
                iconWatcher.EnableRaisingEvents = false;
            }
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            settings.OnSettingsChanged -= Settings_OnSettingsChanged;
            SavePluginSettings(settings);
        }

        private async void Settings_OnSettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings)
        {
            NameFilters = null;
            GameFilters = null;
            if (newSettings.UpdateAutomatically)
            {
                var selected = PlayniteApi.MainView.SelectedGames.ToList();
                PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                PlayniteApi.Database.Games.Update(SetDuplicateState(Visible));
                GameFilters = null;
                NameFilters = null;
                await UpdateIndexAsync();
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
                PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                if (selected?.Count > 0)
                {
                    var reselected = GetCopies(selected.Last())?.FirstOrDefault();
                    if (reselected != null && PlayniteApi.Database.Games.Get(reselected.Id) is Game)
                    {
                        PlayniteApi.MainView.SelectGame(reselected.Id);
                    }
                }
            }
            SourceIconCache.Clear();
            UpdateGuidToCopiesDict();
            SourceSelector.ButtonCaches.ForEach(bc => bc?.Clear());
            GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
            //SavePluginSettings(settings);
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            var toUpdate = new List<Game>();
            var toUpdateGroups = new HashSet<Guid>();
            if (settings.UpdateAutomatically)
            {
                var filter = GetGameFilter();
                UpdateDuplicateState(e.AddedItems.Where(g => g.Name != "New Game").Filter(filter).Where(g => e.AddedItems.Contains(g)), Hidden);
                var nameFilter = GetNameFilter();
                foreach (var game in e.RemovedItems.Filter<IEnumerable<Game>, IFilter<IEnumerable<Game>>>(filter).Where(g => e.RemovedItems.Contains(g)))
                {
                    var name = GetFilteredName(game, nameFilter);
                    if (Index.ContainsKey(name))
                    {
                        List<Guid> guids = Index[name];
                        guids?.ForEach(id => toUpdateGroups.Add(id));
                        if (guids != null && guids.Remove(game.Id) && guids.Count == 1)
                        {
                            if (PlayniteApi.Database.Games.Get(Index[name][0]) is Game last)
                            {
                                if (last.Hidden)
                                {
                                    last.Hidden = false;
                                    toUpdate.Add(last);
                                }
                            }
                            else
                            {
                                Index[name] = null;
                            }
                        }
                        if (Index[name] == null || Index[name].Count == 0)
                        {
                            Index.Remove(name);
                        }
                    }
                }
            }
            if (toUpdate.Count > 0)
            {
                var filter = GetGameFilter();
                var added = e.AddedItems.Where(g => g.Name != "New Game").Filter(filter).Where(g => e.AddedItems.Contains(g));
                var removed = e.RemovedItems.AsEnumerable().Filter(filter).Where(g => e.RemovedItems.Contains(g));
                HashSet<Guid> updatedIds = removed.Concat(added).Select(u => u.Id).Concat(toUpdateGroups).ToHashSet();
                PlayniteApi.Database.Games.Update(toUpdate);
                UpdateGuidToCopiesDict(updatedIds);
                if (Application.Current.Dispatcher.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GroupUpdated?.Invoke(this, updatedIds);
                    });
                } else
                {
                    GroupUpdated?.Invoke(this, updatedIds);
                }
            }
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            IFilter<IEnumerable<Game>> gameFilter = GetGameFilter();
            IFilter<string> nameFilter = GetNameFilter();
            if (settings.AddHiddenToIgnoreList)
            {
                bool dirty = false;
                foreach (var change in e.UpdatedItems)
                {
                    if (change.OldData.Hidden != change.NewData.Hidden)
                    {
                        settings.IgnoredGames.Add(change.NewData.Id);
                        dirty = true;
                    }
                }
                if (dirty)
                {
                    BuildIndex(PlayniteApi.Database.Games, gameFilter, nameFilter);
                    var revealed = SetDuplicateState(Hidden);
                    PlayniteApi.Database.Games.Update(revealed);
                }
                foreach (var change in e.UpdatedItems)
                {
                    if (change.OldData.Hidden != change.NewData.Hidden)
                    {
                        if (Index.TryGetValue(GetFilteredName(change.OldData, nameFilter), out var copies)) {
                            if (copies.Count == 1)
                            {
                                if (PlayniteApi.Database.Games.Get(copies[0]) is Game last)
                                {
                                    if (last.Hidden)
                                    {
                                        last.Hidden = false;
                                        RemoveTag(last, settings.HiddenTagId);
                                        PlayniteApi.Database.Games.Update(last);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            HashSet<Guid> updatedIds = new HashSet<Guid>();
            if (settings.UpdateAutomatically)
            {
                foreach (var change in e.UpdatedItems)
                {
                    var oldData = change.OldData;
                    var newData = change.NewData;
                    var filteredName = GetFilteredName(oldData, nameFilter);
                    if (Index.TryGetValue(filteredName, out var guids))
                    {
                        if (guids.Remove(oldData.Id))
                        {
                            updatedIds.Add(oldData.Id);
                            guids.ForEach(id => updatedIds.Add(id));
                        }
                    }
                }
                var filtered = e.UpdatedItems
                    .Select(u => u.NewData)
                    .Filter(gameFilter)
                    .Intersect(e.UpdatedItems.Select(u => u.NewData));
                foreach (var newData in filtered)
                {
                    var filteredName = GetFilteredName(newData, nameFilter);
                    if (Index.TryGetValue(filteredName, out var guids))
                    {
                        guids.Remove(newData.Id);
                        guids.InsertSorted(newData.Id, GameComparer.Comparer);
                        guids.ForEach(id => updatedIds.Add(id));
                    } else
                    {
                        AddGameToIndex(newData);
                    }
                }
                var toUpdate = SetDuplicateState(Hidden);
                if (toUpdate.Count > 0)
                {
                    PlayniteApi.Database.Games.Update(toUpdate);
                }
            }
            else
            {
                var removeTags = e.UpdatedItems
                    .Where(g => (g.OldData.TagIds?.Contains(settings.HiddenTagId) ?? false) && !g.NewData.Hidden)
                    .Select(g => g.NewData);
                if (removeTags.Count() > 0)
                {
                    removeTags.ForEach(g => RemoveTag(g, settings.HiddenTagId));
                    PlayniteApi.Database.Games.Update(removeTags);
                }
            }
            UpdateGuidToCopiesDict(updatedIds);
            if (Application.Current.Dispatcher.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    GroupUpdated?.Invoke(this, updatedIds);
                });
            }
            else
            {
                GroupUpdated?.Invoke(this, updatedIds);
            }
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
        }

        internal class GameComparer : IComparer<Guid>, IComparer<Game>
        {
            public static GameComparer Comparer = new GameComparer();

            public int Compare(Guid x, Guid y)
            {
                var gameA = Instance.PlayniteApi.Database.Games.Get(x);
                var gameB = Instance.PlayniteApi.Database.Games.Get(y);
                if (Instance.settings.CustomGroups?.FirstOrDefault(g => g.Contains(x)) is CustomGroup group 
                    && group.Contains(y)
                    && group.ScoreByOrder)
                {
                    return group.Games.IndexOf(x).CompareTo(group.Games.IndexOf(y));
                }
                var comp = 0;
                if (gameA != null && gameB != null)
                {
                    List<Guid> gameATags = gameA.TagIds ?? new List<Guid>();
                    List<Guid> gameBTags = gameB.TagIds ?? new List<Guid>();
                    var highPrioTagIds = Instance.settings.HighPriorityTags.Concat(new[] { Instance.settings.HighPrioTagId });
                    var lowPrioTagIds = Instance.settings.LowPriorityTags.Concat(new[] { Instance.settings.LowPrioTagId });
                    if (gameATags.Intersect(highPrioTagIds).Any()
                        && !gameBTags.Intersect(highPrioTagIds).Any())
                    {
                        return -1;
                    }

                    if (gameATags.Intersect(lowPrioTagIds).Any()
                        && !gameBTags.Intersect(lowPrioTagIds).Any())
                    {
                        return 1;
                    }
                    if (!gameATags.Intersect(highPrioTagIds).Any()
                        && gameBTags.Intersect(highPrioTagIds).Any())
                    {
                        return 1;
                    }
                    if (!gameATags.Intersect(lowPrioTagIds).Any()
                        && gameBTags.Intersect(lowPrioTagIds).Any())
                    {
                        return -1;
                    }
                    comp = gameA.CompareTo(gameB, Instance.settings.PriorityProperties);
                }

                //var val = CompareOld(x, y);

                //if (val != comp)
                //{
                //    gameA.CompareTo(gameB, Instance.settings.PriorityProperties);
                //    CompareOld(x, y);
                //}

                return comp;
            }

            private static int CompareOld(Guid x, Guid y)
            {
                int prioX = Instance.GetGamePriority(x);
                int prioY = Instance.GetGamePriority(y);
                int byPriority = prioX.CompareTo(prioY);
                if (byPriority != 0)
                {
                    return byPriority;
                }
                if (Instance.PlayniteApi.Database.Games.Get(x) is Game gameX && Instance.PlayniteApi.Database.Games.Get(y) is Game gameY)
                {
                    var defaultValue = new ReleaseDate(Instance.settings.PreferNewerGame ? DateTime.MinValue : DateTime.MaxValue);
                    var byReleaseDate = (gameY.ReleaseDate ?? defaultValue).CompareTo(gameX.ReleaseDate ?? defaultValue);
                    if (byReleaseDate != 0)
                    {
                        return byReleaseDate * (Instance.settings.PreferNewerGame ? 1 : -1);
                    }
                    return (gameY.Added ?? DateTime.MinValue).CompareTo(gameX.Added ?? DateTime.MinValue) * (Instance.settings.PreferNewerGame ? 1 : -1);
                }
                return 0;
            }

            public int Compare(Game x, Game y)
            {
                return Compare(x.Id, y.Id);
            }
        }

#endregion

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            Settings.PriorityProperties?.AsParallel().ForEach(p => p.Update(PlayniteApi));
            SettingsView = new DuplicateHiderSettingsView();
            return SettingsView;
        }

        public enum Visibility
        {
            Visible,
            Hidden
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return new List<MainMenuItem>
            {
#if DEBUG
                new MainMenuItem
                {
                    MenuSection = "@DuplicateHider",
                    Description = "Benchmark",
                    Action = c =>
                    {
                        var gameFilter = GetGameFilter();
                        var nameFilter = GetNameFilter();
                        var synchronousStopwatch = Stopwatch.StartNew();
                        BuildIndex(PlayniteApi.Database.Games, gameFilter, nameFilter);
                        synchronousStopwatch.Stop();
                        var synchronousIndex = Index;
                        var asynchronousStopwatch = Stopwatch.StartNew();
                        var asynchronousIndex = BuildIndexAsync(PlayniteApi.Database.Games, gameFilter, nameFilter).Result;
                        asynchronousStopwatch.Stop();
                        PlayniteApi.Dialogs.ShowMessage($"Sync: {synchronousStopwatch.Elapsed.TotalMilliseconds}ms\nAsync: {asynchronousStopwatch.Elapsed.TotalMilliseconds}ms");
                    }
                },
#endif
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOC_DH_ExportIndex"),
                    MenuSection = "@|DuplicateHider",
                    Action = (context) =>
                    {
                        SortedDictionary<string, Guid> nameToSharedId = new SortedDictionary<string, Guid>();
                        settings.SharedGameIds.Clear();
                        BuildIndex(PlayniteApi.Database.Games, new PlaceboGameFilter(), GetNameFilter());
                        UpdateGuidToCopiesDict();
                        var export = Index.Where(e => e.Value.Count() > 1).ToDictionary(
                            item => item.Key, 
                            item => item.Value
                                .Select(id => {var game = PlayniteApi.Database.Games.Get(id) ?? new Game(); return new { Name = game.Name, Source = game.Source?.ToString() ?? "Playnite" }; }));

                        var path = PlayniteApi.Dialogs.SaveFile("JSON|*.json|CSV|*.csv");
                        if (path != null)
                        {
                            if (Path.GetExtension(path).EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
                            {
                                using (var file = File.CreateText(path))
                                {
                                    var obj = JsonConvert.SerializeObject(export, Formatting.Indented);
                                    file.Write(obj);
                                }
                            } else if (Path.GetExtension(path).EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var maxCopies = export.Values.Max(v => v.Count());
                                using (var file = File.CreateText(path))
                                {
                                    file.Write("Normalized Name;");
                                    file.WriteLine(string.Join(";", Enumerable.Range(0, maxCopies).Select(i => $"Name {i};Platform {i}")));
                                    foreach(var entry in export)
                                    {
                                        if (entry.Value.Count() > 1)
                                        {
                                            file.Write(entry.Key);
                                            file.Write(";");
                                            file.WriteLine(string.Join(";", entry.Value.Select(g => $"{g.Name};{g.Source}").ToArray()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOC_DH_HideDuplicatesEntry"),
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var hidden = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(hidden);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_NGamesHidden"), hidden.Where(g => g.Hidden).Count()), "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                        UpdateGuidToCopiesDict();
                        GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                    }
                },
                new MainMenuItem
                {
                    Description = PlayniteApi.Resources.GetString("LOC_DH_RevealDuplicatesEntry"),
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Visible);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_NGamesRevealed"), revealed.Where(g => !g.Hidden).Count()), "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                        UpdateGuidToCopiesDict();
                        GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                    }
                }
                //new MainMenuItem
                //{
                //    Description = ResourceProvider.GetString("LOC_DH_AddSelectedToIgnoreEntry"),
                //    MenuSection = "@|DuplicateHider",
                //    Action = (context) => {
                //        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                //        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                //        foreach(var game in PlayniteApi.MainView.SelectedGames)
                //        {
                //            settings.IgnoredGames.Add(game.Id);
                //        }
                //        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                //        if (settings.UpdateAutomatically)
                //        {
                //            var revealed = SetDuplicateState(Hidden);
                //            PlayniteApi.Database.Games.Update(revealed);
                //        }
                //        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                //        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                //        UpdateGuidToCopiesDict();
                //        GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                //    }
                //},
                //new MainMenuItem
                //{
                //    Description = ResourceProvider.GetString("LOC_DH_RemoveSelectedFromIgnoreEntry"),
                //    MenuSection = "@|DuplicateHider",
                //    Action = (context) => {
                //        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                //        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                //        foreach(var game in PlayniteApi.MainView.SelectedGames)
                //        {
                //            settings.IgnoredGames.Remove(game.Id);
                //        }
                //        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                //        if (settings.UpdateAutomatically)
                //        {
                //            var revealed = SetDuplicateState(Hidden);
                //            PlayniteApi.Database.Games.Update(revealed);
                //        }
                //        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                //        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                //        UpdateGuidToCopiesDict();
                //        GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                //    }
                //}
#if DEBUG
                , new MainMenuItem
                {
                    Description = "Serialize Index Data",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        SortedDictionary<string, List<DebugData>> ts = new SortedDictionary<string, List<DebugData>>();
                        foreach(var group in Index.OrderBy(g => g.Key).Where(g => g.Value.Count > 1))
                        {
                            var data = new List<DebugData>();
                            foreach (var copy in group.Value)
                            {
                                if (PlayniteApi.Database.Games.Get(copy) is Game game) {
                                    if (game != null)
                                    {
                                        data.Add(new DebugData() {
                                            Name = game.Name,
                                            Installed = game.IsInstalled,
                                            Score = GetGamePriority(copy),
                                            SourceName = game.GetSourceName(),
                                            SourcePriority = GetSourceRank(game)
                                            }
                                        );
                                    }
                                }
                              }
                            ts.Add(group.Key, data);
                        }
                        var path = PlayniteApi.Dialogs.SaveFile("JSON File|*.json");
                        if (path.Length > 0)
                        {
                            var data = JsonConvert.SerializeObject(ts, Formatting.Indented);
                            File.WriteAllText(path, data);
                            System.Diagnostics.Process.Start(path);
                        }
                    }
                }
#endif
            };
        }
#if DEBUG
        struct DebugData
        {
            public string Name;
            public string SourceName;
            public int SourcePriority;
            public int Score;
            public bool Installed;
        };
#endif
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (editGamesCommand is ICommand && args.Games.Count == 1)
            {
                Game mainCopy = args.Games.FirstOrDefault();
                var otherCopies = GetOtherCopies(mainCopy);
                if (otherCopies.Count > 0)
                {
                    yield return new GameMenuItem
                    {
                        Description = ResourceProvider.GetString("LOC_DH_EditAllCopies"),
                        Icon = "EditGameIcon",
                        Action = (arg) =>
                        {
                            if (arg.Games.Count > 0)
                            {
                                IEnumerable<Guid> gameIds = (new[] { mainCopy.Id }).Concat(otherCopies.Select(g => g.Id));
                                PlayniteApi.MainView.SelectGames(gameIds);
                                editGamesCommand?.Execute(null);
                                PlayniteApi.MainView.SelectGames(new[] { mainCopy.Id });
                            }
                        }
                    };
                }
            }

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_DH_CopyFieldsFromGame"),
                MenuSection = "DuplicateHider",
                Action = (arg) =>
                {
                    List<CopyFieldsModel> copyFieldsModels = new List<CopyFieldsModel>();
                    foreach(var game in arg.Games)
                    {
                        var copies = GetOtherCopies(game);
                        if (copies.Count > 0)
                        {
                            copyFieldsModels.Add(new CopyFieldsModel(game, copies));
                        }
                    }

                    var viewModel = new CopyFieldsViewModel(copyFieldsModels);

                    var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
                    window.Title = "DuplicateHider";
                    window.Width = 700;
                    window.Height = 650;
                    window.Content = new CopyFieldsView(viewModel);
                    window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.Name == "WindowMain");
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Show();
                }
            };

            if (settings.ShowOtherCopiesInGameMenu)
            {
                if (args.Games.Count == 1)
                {
                    var selected = args.Games[0];
                    var others = GetOtherCopies(selected);
                    string menuSection = string.Format(ResourceProvider.GetString("LOC_DH_N_OtherCopies"), others.Count());
                    foreach (var copy in others)
                    {
                        yield return new GameMenuItem
                        {
                            Action = context => PlayniteApi.StartGame(copy.Id),
                            MenuSection = menuSection,
                            Description = ExpandDisplayString(copy, settings.DisplayString)
                        };
                    }
                }
            }
            // Create new group and add selected
            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_AddSelectedToNewCustomGroup"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_CustomGroups")}",
                Action = CreateNewGroup
            };
            // Add to existing group
            foreach (var group in settings.CustomGroups)
            {
                yield return new GameMenuItem()
                {
                    Description = group.Name,
                    MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_CustomGroups")}|{ResourceProvider.GetString("LOC_DH_AddSelectedToExistingCustomGroup")}",
                    Action = context =>
                    {
                        AddToGroup(context, group);
                    }
                };
            }
            // Remove from existing group
            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_RemoveFromExistingGroup"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_CustomGroups")}",
                Action = RemoveFromGroups
            };

            // Remove group with selected games inside
            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_DisbandGroupOfSelected"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_CustomGroups")}",
                Action = DisbandGroups
            };

            // Remove group with selected games inside
            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_AssignOverrideTagHigh"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_AssignOverrideTag")}",
                Action = a =>
                {
                    foreach(var game in a.Games)
                    {
                        if (game.TagIds == null)
                        {
                            game.TagIds = new List<Guid>();
                        }
                        game.TagIds.Remove(settings.LowPrioTagId);
                        game.TagIds.AddMissing(settings.HighPrioTagId);
                    }
                    PlayniteApi.Database.Games.Update(a.Games);
                    PlayniteApi.MainView.SelectGames(a.Games.Select(g => g.Id));
                }
            };

            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_AssignOverrideTagLow"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_AssignOverrideTag")}",
                Action = a =>
                {
                    foreach (var game in a.Games)
                    {
                        if (game.TagIds == null)
                        {
                            game.TagIds = new List<Guid>();
                        }
                        game.TagIds.Remove(settings.HighPrioTagId);
                        game.TagIds.AddMissing(settings.LowPrioTagId);
                    }
                    PlayniteApi.Database.Games.Update(a.Games);
                    PlayniteApi.MainView.SelectGames(a.Games.Select(g => g.Id));
                }
            };

            yield return new GameMenuItem()
            {
                Description = ResourceProvider.GetString("LOC_DH_AssignOverrideTagClear"),
                MenuSection = $"DuplicateHider|{ResourceProvider.GetString("LOC_DH_AssignOverrideTag")}",
                Action = a =>
                {
                    var updated = new List<Game>();
                    foreach(var game in a.Games)
                    {
                        updated.Add(game);
                        game.TagIds?.Remove(settings.HighPrioTagId);
                        game.TagIds?.Remove(settings.LowPrioTagId);
                        foreach(var oldcopy in GetOtherCopies(game))
                        {
                            var copy = PlayniteApi.Database.Games.Get(oldcopy.Id);
                            copy.TagIds?.Remove(settings.HighPrioTagId);
                            copy.TagIds?.Remove(settings.LowPrioTagId);
                            updated.Add(copy);
                        }
                    }
                    PlayniteApi.Database.Games.Update(updated);
                    PlayniteApi.MainView.SelectGames(a.Games.Select(g => g.Id));
                }
            };
            
            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_DH_AddSelectedToIgnoreEntry"),
                MenuSection = "DuplicateHider",
                Action = context => {
                    PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                    foreach (var game in context.Games)
                    {
                        settings.IgnoredGames.Add(game.Id);
                    }
                    BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                    if (settings.UpdateAutomatically)
                    {
                        var revealed = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                    }
                    PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    UpdateGuidToCopiesDict();
                    GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                }
            };

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_DH_AddSelectedToIgnoreEntryAndReveal"),
                MenuSection = "DuplicateHider",
                Action = context => {
                    PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                    foreach (var game in context.Games)
                    {
                        settings.IgnoredGames.Add(game.Id);
                    }
                    BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                    if (settings.UpdateAutomatically)
                    {
                        var revealed = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                    }
                    context.Games.ForEach(g => g.Hidden = false);
                    PlayniteApi.Database.Games.Update(context.Games);
                    PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    UpdateGuidToCopiesDict();
                    GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                }
            };

            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOC_DH_RemoveSelectedFromIgnoreEntry"),
                MenuSection = "DuplicateHider",
                Action = context =>
                {
                    PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                    foreach (var game in PlayniteApi.MainView.SelectedGames)
                    {
                        settings.IgnoredGames.Remove(game.Id);
                    }
                    BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                    if (settings.UpdateAutomatically)
                    {
                        var revealed = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                    }
                    PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                    PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    UpdateGuidToCopiesDict();
                    GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
                }
            };
        }

        private void DisbandGroups(GameMenuItemActionArgs context)
        {
            var oldGroups = settings.CustomGroups;
            var newGroups = oldGroups.Where(g => !context.Games.Any(game => g.Contains(game.Id))).ToList();
            settings.CustomGroups = newGroups;
            int count = oldGroups.Count - newGroups.Count;
            PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_N_GroupsDisbanded"), count));
            if (count > 0)
            {
                Settings_OnSettingsChanged(settings, settings);
            }
        }

        private void RemoveFromGroups(GameMenuItemActionArgs context)
        {
            int removedGames = 0;
            foreach (var group in settings.CustomGroups)
            {
                foreach (var game in context.Games)
                {
                    if (group.RemoveGame(game))
                    {
                        removedGames++;
                    }
                }
            }
            settings.CustomGroups = settings.CustomGroups.Where(g => g.Count() > 0).ToList();
            PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_N_GamesRemovedfromGroup"), removedGames));
            if (removedGames > 0)
            {
                Settings_OnSettingsChanged(settings, settings);
            }
        }

        private void AddToGroup(GameMenuItemActionArgs context, CustomGroup group)
        {
            var prevCount = group.Games.Count();
            if (ResolveGroupConflicts(context.Games, group))
            {
                int count = group.Games.Count() - prevCount;
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_N_GamesAddedToGroup"), count, group.Name));
                if (count > 0)
                {
                    Settings_OnSettingsChanged(settings, settings);
                }
            }

        }

        private void CreateNewGroup(GameMenuItemActionArgs context)
        {
            var newGroup = new CustomGroup();
            if (ResolveGroupConflicts(context.Games, newGroup))
            {
                var name = PlayniteApi.Dialogs.SelectString(
                    ResourceProvider.GetString("LOC_DH_CustomGroupNamePrompt")??"",
                    ResourceProvider.GetString("LOC_DH_CustomGroupName")??"",
                    context.Games.FirstOrDefault()?.Name??"");
                if (name.Result)
                {
                    newGroup.Name = name.SelectedString;
                    settings.CustomGroups.Add(newGroup);
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOC_DH_N_GamesAddedToGroup"), newGroup.Games.Count(), newGroup.Name));
                    Settings_OnSettingsChanged(settings, settings);
                }
            }
        }

        private bool ResolveGroupConflicts(IEnumerable<Game> games, CustomGroup newGroup)
        {
            var doubles = games.Where(game => settings.CustomGroups.Where(g => g != newGroup).Any(g => g.Contains(game)));
            if (doubles.Any())
            {
                MessageBoxOption moveOption = new MessageBoxOption(ResourceProvider.GetString("LOC_DH_MoveButton"), true, false);
                MessageBoxOption keepOption = new MessageBoxOption(ResourceProvider.GetString("LOC_DH_KeepButton"), false, false);
                MessageBoxOption cancelOption = new MessageBoxOption(ResourceProvider.GetString("LOCCancelLabel"), false, true);
                var option = PlayniteApi.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOC_DH_GroupConflictDescription"), doubles.Select(g => g.Name).Aggregate((a, b) => $"{a}\n{b}")),
                    ResourceProvider.GetString("LOC_DH_GroupConflict"),
                    MessageBoxImage.Error,
                    new List<MessageBoxOption>()
                    {
                                moveOption,
                                keepOption,
                                cancelOption,
                    });
                if (option == cancelOption)
                {
                    return false;
                }
                if (option == moveOption)
                {
                    foreach (var conflict in doubles)
                    {
                        var existing = settings.CustomGroups.First(g => g.Contains(conflict));
                        existing.Transfer(newGroup, conflict.Id);
                    }
                }
            }
            var singles = games.Where(game => settings.CustomGroups.All(g => !g.Contains(game)));
            if (singles.Any())
            {
                foreach (var game in singles)
                {
                    newGroup.AddGame(game);
                }
                return true;
            } 
            return false;
        }

        public bool AddTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.AddMissing(tagId);
                } else
                {
                    game.TagIds = new List<Guid> { tagId };
                }
            }
            return false;
        }

        public bool RemoveTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.Remove(tagId);
                }
            }
            return false;
        }

        public List<Game> GetOtherCopies(Game game)
        {
            return GetCopies(game)?.Where(g => g.Id != (game?.Id ?? Guid.Empty)).ToList();
        }

        public List<Game> GetCopies(Game game)
        {
            if (GuidToCopies.TryGetValue(game.Id, out var copies))
            {
                return copies;
            }
            return new List<Game> { game };
        }

        private int AddGameToIndex(Game game)
        {
            var nameFilter = GetNameFilter();

            var name = GetFilteredName(game, nameFilter);
            if (Index.ContainsKey(name))
            {
                Index[name].Remove(game.Id);
            }
            else
            {
                Index[name] = new List<Guid> { };
            }
            Index[name].InsertSorted(game.Id, GameComparer.Comparer);
            return Index[name].IndexOf(game.Id);
        }

        private void UpdateDuplicateState(IEnumerable<Game> games, Visibility visibility)
        {
            var nameFilter = GetNameFilter();
            foreach (var game in games)
            {
                var name = GetFilteredName(game, nameFilter);
                if (Index.ContainsKey(name))
                {
                    Index[name].Remove(game.Id);
                }
                else
                {
                    Index[name] = new List<Guid> { };
                }
                Index[name].InsertSorted(game.Id, GameComparer.Comparer);
            }
            var toUpdate = SetDuplicateState(visibility);
            if (toUpdate.Count > 0)
            {
                PlayniteApi.Database.Games.Update(toUpdate);
            }
        }

        private IList<Game> SetDuplicateState(Visibility visibility)
        {
            List<Game> toUpdate = new List<Game> { };
            bool hidden = visibility == Hidden;
            foreach (var copies in Index.Values)
            {
                for (int i = 1; i < copies.Count; ++i)
                {
                    if (PlayniteApi.Database.Games.Get(copies[i]) is Game copy)
                    {
                        if (copy.Hidden != hidden)
                        {
                            copy.Hidden = hidden;
                            if (hidden)
                            {
                                AddTag(copy, settings.HiddenTagId);
                            } else
                            {
                                RemoveTag(copy, settings.HiddenTagId);
                            }
                            toUpdate.Add(copy);
                        }
                    }
                }
                if (copies.Count > 0 && PlayniteApi?.Database?.Games?.Get(copies[0]) is Game game)
                {
                    if (game.Hidden && (game.TagIds?.Contains(settings.HiddenTagId) ?? false))
                    {
                        game.Hidden = false;
                        RemoveTag(game, settings.HiddenTagId);
                        toUpdate.Add(game);
                    }
                }
            }
            return toUpdate;
        }

        private void BuildIndex(IEnumerable<Game> games, IFilter<IEnumerable<Game>> gameFilter, IFilter<string> nameFilter)
        {
            //Index = BuildIndexAsync(games, gameFilter, nameFilter).Result;
            Index.Clear();
            foreach (var game in games.Filter(gameFilter))
            {
                var cleanName = GetFilteredName(game, nameFilter);
                if (!Index.ContainsKey(cleanName))
                {
                    Index[cleanName] = new List<Guid> { };
                }

                Index[cleanName].InsertSorted(game.Id, GameComparer.Comparer);
            }
        }

        public async Task UpdateIndexAsync()
        {
            Index = await BuildIndexAsync(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
        }

        public void UpdateIndex()
        {
            BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
        }

        private async Task<Dictionary<string, List<Guid>>> BuildIndexAsync(IEnumerable<Game> games, IFilter<IEnumerable<Game>> gameFilter, IFilter<string> nameFilter)
        {
            var numberOfThreads = Environment.ProcessorCount;
            var minNumberOfItems = 500;
            var maxDepth = (int)Math.Round(Math.Log(numberOfThreads, 2));

            return await PartitionAndMergeIndexAsync(new ArraySegment<Game>(games.Filter(gameFilter).ToArray()), nameFilter, maxDepth, minNumberOfItems).ConfigureAwait(false);
        }

        private async Task<Dictionary<string, List<Guid>>> PartitionAndMergeIndexAsync(ArraySegment<Game> games, IFilter<string> nameFilter, int maxDepth, int minItems)
        {
            var count = games.Count;
            if (maxDepth <= 0 || count <= minItems)
            {
                return await BuildPartialIndexAsync(games, nameFilter).ConfigureAwait(false);
            }
            return await Task.Run(async () =>
            {
                var partA = new ArraySegment<Game>(games.Array, games.Offset, count / 2);
                var partB = new ArraySegment<Game>(games.Array, games.Offset + partA.Count, count - partA.Count);
                var a = PartitionAndMergeIndexAsync(partA, nameFilter, maxDepth - 1, minItems);
                var b = PartitionAndMergeIndexAsync(partB, nameFilter, maxDepth - 1, minItems);
                await Task.WhenAll(a, b).ConfigureAwait(false);
                return MergeIndexInto(a.Result, b.Result);
            }).ConfigureAwait(false);
        }

        private async Task<Dictionary<string, List<Guid>>> BuildPartialIndexAsync(ArraySegment<Game> games, IFilter<string> nameFilter)
        {
            return await Task.Run(() =>
            {
                var part = new Dictionary<string, List<Guid>>();
                foreach (var game in games)
                {
                    var cleanName = GetFilteredName(game, nameFilter);
                    if (!part.ContainsKey(cleanName))
                    {
                        part[cleanName] = new List<Guid> { };
                    }

                    part[cleanName].InsertSorted(game.Id, GameComparer.Comparer);
                }
                return part;
            }).ConfigureAwait(false);
        }

        private Dictionary<string, List<Guid>> MergeIndexInto(Dictionary<string, List<Guid>> from, Dictionary<string, List<Guid>> into)
        {
            foreach (var entry in from)
            {
                var cleanName = entry.Key;
                foreach(var id in entry.Value)
                {
                    List<Guid> existing = null;
                    if (into.TryGetValue(cleanName, out var guids))
                    {
                        existing = guids;
                    } else
                    {
                        existing = new List<Guid> { };
                        into[cleanName] = existing;
                    }
                    existing.InsertSorted(id, GameComparer.Comparer);
                }
            }
            return into;
        }

        static readonly Regex regexVariable = new Regex(@"{(?:(?<Prefix>[^'{}]*)')?(?<Variable>[^'{}]+)(?:'(?<Suffix>[^'{}]*))?}");
        static readonly int prefixIdx = regexVariable.GroupNumberFromName("Prefix");
        static readonly int suffixIdx = regexVariable.GroupNumberFromName("Suffix");
        static readonly int variableIdx = regexVariable.GroupNumberFromName("Variable");

        public static IList<KeyValuePair<string, string>> GetGameVariables()
        {
            var vars = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Installed", "{'Installed'}"),
                new KeyValuePair<string, string>("SourceName", "{'SourceName'}"),
                new KeyValuePair<string, string>("Regions", "{'Regions'}"),
                new KeyValuePair<string, string>("PlayActionName", "{'PlayActionName'}"),
                new KeyValuePair<string, string>("PlayActionFileName", "{'PlayActionFileName'}"),
            };


            foreach (var variable in typeof(ExpandableVariables).GetFields())
            {
                vars.Add(new KeyValuePair<string, string>(variable.Name, ((string)variable.GetRawConstantValue()).Replace("{", "{'").Replace("}", "'}")));
            }

            var t = typeof(Game);

            foreach (var variable in typeof(Game).GetProperties())
            {
                if (!vars.TryFind(s => s.Key.Contains(variable.Name) || s.Value.Contains(variable.Name), out var _))
                    if (variable.PropertyType == typeof(string) || variable.PropertyType == typeof(int?) || variable.PropertyType == typeof(float?) || variable.PropertyType == typeof(DateTime?))
                        vars.Add(new KeyValuePair<string, string>(variable.Name, "{'" + variable.Name + "'}"));
            }

            return vars;
        }

        public string ExpandDisplayString(Game game, string displayString)
        {
            if (game == null || displayString == null) return string.Empty;
            var result = displayString;
            const int MAX_RECURSION = 5;
            int recursion = 0;
            while (regexVariable.IsMatch(result) && MAX_RECURSION > recursion)
            {
                var matches = regexVariable.Matches(result);
                foreach (Match match in matches)
                {
                    var prefix = match.Groups[prefixIdx].Value;
                    var suffix = match.Groups[suffixIdx].Value;
                    var infix = match.Groups[variableIdx].Value;
                    var variable = "{" + infix + "}";
                    var expanded = PlayniteApi.ExpandGameVariables(game, variable);
                    if (expanded.Length == 0 || expanded == variable)
                    {
                        expanded = expanded.Replace("{Source}", game.GetSourceName());
                        expanded = expanded.Replace("{Installed}", game.IsInstalled ? ResourceProvider.GetString("LOCGameIsInstalledTitle") : ResourceProvider.GetString("LOCGameIsUnInstalledTitle"));
                        if (expanded.Contains("{Regions}"))
                        {
                            expanded = expanded.Replace("{Regions}", game.Regions != null ? string.Join(", ", game.Regions.Select(e => e.Name)) : "");
                        }
                        if (game.GameActions?.FirstOrDefault(a => a.IsPlayAction) is GameAction playAction && game.PluginId == Guid.Empty)
                        {
                            expanded = expanded.Replace("{PlayActionName}", playAction.Name ?? "");
                            expanded = expanded.Replace("{PlayActionFileName}", Path.GetFileNameWithoutExtension(playAction.Path) ?? "");

                        } else
                        {
                            expanded = expanded.Replace("{PlayActionName}", "");
                            expanded = expanded.Replace("{PlayActionFileName}", "");
                        }

                        var type = typeof(Game).GetFields();
                        foreach (var field in typeof(Game).GetProperties())
                        {
                            if (field.PropertyType == typeof(string) || field.PropertyType == typeof(int?) || field.PropertyType == typeof(float?) || field.PropertyType == typeof(DateTime?))
                            {
                                if (field.PropertyType == typeof(DateTime?))
                                {
                                    expanded = expanded.Replace("{" + field.Name + "}", ((DateTime?)field.GetValue(game))?.ToString("d") ?? string.Empty);
                                } else
                                {
                                    expanded = expanded.Replace("{" + field.Name + "}", field.GetValue(game)?.ToString()??string.Empty);
                                }
                            }
                        }
                    }
                    if (expanded.Length != 0 && expanded != variable)
                    {
                        result = result.Replace(match.Value, prefix + expanded + suffix);
                    }
                    else
                    {
                        result = result.Replace(match.Value, string.Empty);
                    }
                }
                ++recursion;
            }
            return result.Trim().Replace("_", " \u0331 ");
        }

        public int GetGamePriority(Guid id)
        {
            var rankRange = settings.Priorities.Count;
            if (settings.CustomGroups.FirstOrDefault(g => g.Contains(id)) is CustomGroup group &&
                group.ScoreByOrder)
            {
                return group.Games.IndexOf(id);
            }
            if (PlayniteApi.Database.Games.Get(id) is Game game)
            {
                var offset = 0;
                var tags = game.TagIds ?? Enumerable.Empty<Guid>();
                if (tags.Contains(settings.HighPrioTagId))
                {
                    offset -= 2 * rankRange;
                }
                if (tags.Contains(settings.LowPrioTagId))
                {
                    offset += 2 * rankRange;
                }
                return GetSourceRank(game) - (rankRange * (game.IsInstalled ? 1 : 0)) + offset;
            }
            return rankRange;
        }

        public int GetSourceRank(Game game)
        {
            int index = settings.Priorities.IndexOf(game.GetSourceName());
            if (index > -1)
            {
                return index;
            }
            else
            {
                return settings.Priorities.Count;
            }
        }

        IFilter<string> NameFilters = null;

        IFilter<string> GetNameFilter()
        {
            if (NameFilters == null)
            {
                var customRules = IFilter<string>.MakeChain(settings.ReplaceFilters.Cast<IFilter<string>>().ToList());
                NameFilters = customRules.Append(
                    IFilter<string>.MakeChain
                    (
                        new NumberToRomanFilter(),
                        new DiacriticsFilter(),
                        new CaseFilter(CaseFilter.Case.Lower),
                        new WhiteSpaceFilter(),
                        new ReplaceFilter("and", "&", "+"),
                        new ReplaceFilter("goty", "gameoftheyearedition", "gotyedition", "gameoftheyear"),
                        new SpecialCharFilter()
                    )
                );
            }
            return NameFilters;
        }

        public string GetFilteredName(Game game, IFilter<string> filter)
        {
            if (settings.CustomGroups.FirstOrDefault(g => g.Contains(game)) is CustomGroup customGroup)
            {
                return customGroup.Id.ToString();
            }
            return game.Name.Filter(filter);
        }

        static IFilter<IEnumerable<Game>> GameFilters = null;

        IFilter<IEnumerable<Game>> GetGameFilter()
        {
            if (GameFilters is null)
            {
                GameFilters = IFilter<IEnumerable<Game>>.MakeChain(
                    new NameNullFilter(),
                    new PlatformFilter(true, settings.IncludePlatforms),
                    new SourceFilter(false, settings.ExcludeSources),
                    new CategoryFilter(false, settings.ExcludeCategories),
                    new IgnoreFilter(settings.IgnoredGames),
                    new UnionFilter(settings.CustomGroups.SelectMany(group => group.Games.Select(id => PlayniteApi.Database.Games.Get(id)).OfType<Game>()))
                );
            }
            return GameFilters;
        }

        const string LibraryStatisticsId = "LibraryStatistics";

        public StartPageExtensionArgs GetAvailableStartPageViews()
        {
            var views = new List<StartPageViewArgsBase> { 
                new StartPageViewArgsBase{ Name = "Library Statistics", ViewId = LibraryStatisticsId }
            };
            return new StartPageExtensionArgs { ExtensionName = "DuplicateHider", Views = views };
        }

        public object GetStartPageView(string viewId, Guid instanceId)
        {
            if (viewId == LibraryStatisticsId)
            {
                return new Views.StartPage.LibraryStatisticsView { DataContext = new ViewModels.LibraryStatisticsViewModel(this) };
            }
            return null;
        }

        public Control GetStartPageViewSettings(string viewId, Guid instanceId)
        {
            return null;
        }

        public void OnViewRemoved(string viewId, Guid instanceId)
        {
            
        }
    }
}