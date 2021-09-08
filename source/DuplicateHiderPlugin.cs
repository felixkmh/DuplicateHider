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


[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider
{
    public class DuplicateHiderPlugin : GenericPlugin
    {
        public event EventHandler<IEnumerable<Guid>> GroupUpdated;
        internal struct GameSelectedArgs { public Guid? oldId; public Guid? newId; }
        internal event EventHandler<GameSelectedArgs> GameSelected;

        private readonly ILogger logger;

        internal DuplicateHiderSettings settings { get; private set; }
        internal static IPlayniteAPI API { get; private set; } = null;

        public static DuplicateHiderPlugin Instance { get; private set; }

        public override Guid Id { get; } = Guid.Parse("382f8003-8ed0-4e47-ae93-05b43c9c6c32");

        private Dictionary<string, List<Guid>> Index { get; set; } = new Dictionary<string, List<Guid>>();
        private Dictionary<Guid, List<Game>> GuidToCopies { get; set; } = new Dictionary<Guid, List<Game>>();

        internal static readonly IconCache SourceIconCache = new IconCache();

        public DuplicateHiderSettingsView SettingsView { get; private set; }

        private FileSystemWatcher iconWatcher = null;

        public Guid? CurrentlySelected { get; private set; } = null;

        public DuplicateHiderPlugin(IPlayniteAPI api) : base(api)
        {
            logger = LogManager.GetLogger();
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

            AddSettingsSupport(new AddSettingsSupportArgs { SettingsRoot = "settings", SourceName = "DuplicateHider" });
        }

        internal void UpdateGuidToCopiesDict()
        {
            GuidToCopies.Clear();
            foreach(var pair in Index)
            {
                var copies = pair.Value.Select(id => PlayniteApi.Database.Games.Get(id)).Where(g => g is Game).ToList();
                foreach(var id in pair.Value)
                {
                    GuidToCopies.Add(id, copies);
                }
            }
        }

        static int GeneratedElements = 0;

        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (settings.EnableUiIntegration)
            {
                if (args.Name=="SourceSelector")
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
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
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

            // Clean orphaned entries from Priorites list
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

            BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
            GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
            if (settings.UpdateAutomatically)
            {
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
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
                ).Select(s=>s.Name);
                if (foundThemIcons.Count() > 0)
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "DHFOUNDTHEMEICONS", $"DuplicateHider:\nFound Icons in current Theme, but \"Theme Icon\" option is disabled.\nFound icons for " +
                            $"{string.Join(", ", foundThemIcons)}. " +
                            $"{(settings.PreferUserIcons?"\nMight be overriden by user icons because of \"Prefer User Icons\" option.":"")}" +
                            $"\nClick here for a preview.", 
                        NotificationType.Info, () => 
                        {
                            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                            {
                                ShowMinimizeButton = false, ShowCloseButton = true, ShowMaximizeButton = false
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
                if (AppDomain.CurrentDomain.GetAssemblies()
             .Select(asm => asm.GetName())
             .Any(asm => asm.Name == "QuickSearchSDK" && asm.Version.Major == 1 && asm.Version.Minor == 4))
                {
                    QuickSearch.QuickSearchSDK.AddPluginSettings("DuplicateHider", settings, OpenSettingsView);
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
                
            } catch(Exception)
            {

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
                    GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"Name={e.Name}, Type={e.ChangeType}"); 
#endif
                }
            }
        }

        public string GetUserIconFolderPath()
        {
            return Path.Combine(
                            GetPluginUserDataPath(),
                            "source_icons"
                        );
        }

        public void SelectGame(Guid? gameId)
        {
            if (gameId is Guid id)
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
            var oldId = args.OldValue?.FirstOrDefault()?.Id;
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

        private void Settings_OnSettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings)
        {
            NameFilters = null;
            GameFilters = null;
            if (newSettings.UpdateAutomatically)
            {
                PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                PlayniteApi.Database.Games.Update(SetDuplicateState(Visible));
                GameFilters = null;
                NameFilters = null;
                BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
                PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            }
            SourceIconCache.Clear();
            UpdateGuidToCopiesDict();
            SourceSelector.ButtonCaches.ForEach(bc => bc?.Clear());
            GroupUpdated?.Invoke(this, PlayniteApi.Database.Games.Select(g => g.Id));
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            var toUpdate = new List<Game>();
            if (settings.UpdateAutomatically)
            {
                var filter = GetGameFilter();
                UpdateDuplicateState(e.AddedItems.Where(g=>g.Name != "New Game").Filter<IEnumerable<Game>, IFilter<IEnumerable<Game>>>(filter), Hidden);
                var nameFilter = GetNameFilter();
                foreach (var game in e.RemovedItems.Filter<IEnumerable<Game>, IFilter<IEnumerable<Game>>>(filter))
                {
                    var name = game.Name.Filter(nameFilter);
                    if (Index.ContainsKey(name))
                    {
                        if (Index[name].Remove(game.Id) && Index[name].Count == 1)
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
                    }
                }
            }
            PlayniteApi.Database.Games.Update(toUpdate);
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            UpdateGuidToCopiesDict();
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
                        if (Index.TryGetValue(nameFilter.Apply(change.OldData.Name), out var copies)) {
                            if (copies.Count == 1)
                            {
                                if (PlayniteApi.Database.Games.Get(copies[0]) is Game last)
                                {
                                    if (last.Hidden)
                                    {
                                        last.Hidden = false;
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
                foreach (var change in  e.UpdatedItems)
                {
                    var oldData = change.OldData;
                    var newData = change.NewData;
                    var filteredName = oldData.Name.Filter(nameFilter);
                    if (Index.TryGetValue(filteredName, out var guids))
                    {
                        if (guids.Remove(oldData.Id))
                        {
                            updatedIds.Add(oldData.Id);
                            var filtered = (new Game[] { oldData, newData }).AsEnumerable().Filter(gameFilter);
                            if (filtered.Count() < 2)
                            {
                                if (newData.Hidden)
                                {
                                    newData.Hidden = false;
                                    PlayniteApi.Database.Games.Update(newData);
                                }
                            }
                            if (guids.Count == 1)
                            {
                                if (PlayniteApi.Database.Games.Get(guids[0]) is Game game)
                                {
                                    game.Hidden = false;
                                    PlayniteApi.Database.Games.Update(game);
                                }
                            }
                            guids.ForEach(id => updatedIds.Add(id));
                        }
                    }
                }
                foreach (var newData in (from update in e.UpdatedItems select update.NewData).Filter(gameFilter))
                {
                    var filteredName = newData.Name.Filter(nameFilter);
                    if (Index.TryGetValue(filteredName, out var guids))
                    {
                        guids.InsertSorted(newData.Id, GameComparer.Comparer);
                        guids.ForEach(id => updatedIds.Add(id));
                    }
                }
                GroupUpdated?.Invoke(this, updatedIds);
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
            }
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            UpdateGuidToCopiesDict();
        }

        private class GameComparer : Comparer<Guid>
        {
            public static GameComparer Comparer = new GameComparer();

            public override int Compare(Guid x, Guid y)
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
                        return byReleaseDate * (Instance.settings.PreferNewerGame? 1 : -1);
                    }
                    return (gameY.Added ?? DateTime.MinValue).CompareTo(gameX.Added ?? DateTime.MinValue) * (Instance.settings.PreferNewerGame ? 1 : -1);
                }
                return 0;
            }
        }

        #endregion

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
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
                    Description = "Generate Shared Ids",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) =>
                    {
                        SortedDictionary<string, Guid> nameToSharedId = new SortedDictionary<string, Guid>();
                        settings.SharedGameIds.Clear();
                        BuildIndex(PlayniteApi.Database.Games, new PlaceboGameFilter(), GetNameFilter());
                        foreach (var group in Index)
                        {
                            var sharedId = Guid.NewGuid();
                            nameToSharedId.Add(group.Key, sharedId);
                            foreach(var id in group.Value)
                            {
                                settings.SharedGameIds.Add(id, sharedId);
                            }
                        }
                        var path = Path.Combine(this.GetPluginUserDataPath(), "SharedGameIds.json");
                        using (var file = File.CreateText(path))
                        {
                            var obj = JsonConvert.SerializeObject(settings.SharedGameIds, Formatting.Indented);
                            file.Write(obj);
                        }
                        path = Path.Combine(this.GetPluginUserDataPath(), "NameToSharedId.json");
                        using (var file = File.CreateText(path))
                        {
                            var obj = JsonConvert.SerializeObject(nameToSharedId, Formatting.Indented);
                            file.Write(obj);
                        }
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                    }
                },
#endif
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
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOC_DH_AddSelectedToIgnoreEntry"),
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        foreach(var game in PlayniteApi.MainView.SelectedGames) { settings.IgnoredGames.Add(game.Id); } BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                },
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOC_DH_RemoveSelectedFromIgnoreEntry"),
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        foreach(var game in PlayniteApi.MainView.SelectedGames) { settings.IgnoredGames.Remove(game.Id); } BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                }
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

            var entries = new List<GameMenuItem>();
            if (settings.ShowOtherCopiesInGameMenu)
            {
                if (args.Games.Count == 1)
                {
                    var selected = args.Games[0];
                    var others = GetOtherCopies(selected);
                    string menuSection = string.Format(ResourceProvider.GetString("LOC_DH_N_OtherCopies"), others.Count());
                    foreach (var copy in others)
                    {
                        entries.Add(new GameMenuItem
                        {
                            Action = context => PlayniteApi.StartGame(copy.Id),
                            MenuSection = menuSection,
                            Description = ExpandDisplayString(copy, settings.DisplayString)
                        });
                    }
                }
            }

            return entries;
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

        private void UpdateDuplicateState(IEnumerable<Game> games, Visibility visibility)
        {
            var nameFilter = GetNameFilter();
            foreach (var game in games)
            {
                var name = game.Name.Filter(nameFilter);
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
            PlayniteApi.Database.Games.Update(SetDuplicateState(visibility));
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
                            toUpdate.Add(copy);
                        }
                    }
                }
                if (copies.Count > 1 && PlayniteApi.Database.Games.Get(copies[0]) is Game game)
                {
                    if (game.Hidden)
                    {
                        game.Hidden = false;
                        toUpdate.Add(game);
                    }
                }
            }
            return toUpdate;
        }

        private void BuildIndex(IEnumerable<Game> games, IFilter<IEnumerable<Game>> gameFilter, IFilter<string> nameFilter)
        {
            Index.Clear();
            foreach (var game in games.Filter(gameFilter))
            {
                var cleanName = game.Name.Filter(nameFilter);
                if (!Index.ContainsKey(cleanName))
                {
                    Index[cleanName] = new List<Guid> { };
                }

                Index[cleanName].InsertSorted(game.Id, GameComparer.Comparer);
            }
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
                new KeyValuePair<string, string>("SourceName", "{'SourceName'}")
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

        public async Task<string> ExpandDisplayStringAsync(Game game, string displayString)
        {
            return ExpandDisplayString(game, displayString);
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
                        expanded = expanded.Replace("{Installed}", game.IsInstalled ? "Installed" : "Not installed");
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
            if (PlayniteApi.Database.Games.Get(id) is Game game)
            {
                return GetSourceRank(game) - (rankRange * (game.IsInstalled ? 1 : 0));
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

        static IFilter<IEnumerable<Game>> GameFilters = null;

        IFilter<IEnumerable<Game>> GetGameFilter()
        {
            if (GameFilters is null)
            {
                GameFilters = IFilter<IEnumerable<Game>>.MakeChain(
                    new PlatformFilter(true, settings.IncludePlatforms),
                    new SourceFilter(false, settings.ExcludeSources),
                    new CategoryFilter(false, settings.ExcludeCategories),
                    new IgnoreFilter(settings.IgnoredGames)
                );
            }
            return GameFilters;
        }
    }
}