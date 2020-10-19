using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using static DuplicateHider.DuplicateHider.Visibility;

namespace DuplicateHider
{
    public class DuplicateHider : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private DuplicateHiderSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("382f8003-8ed0-4e47-ae93-05b43c9c6c32");

        private Dictionary<string, List<Guid>> index { get; set; } = new Dictionary<string, List<Guid>>();
        public DuplicateHiderSettingsView SettingsView { get; private set; }

        public DuplicateHider(IPlayniteAPI api) : base(api)
        {
            settings = new DuplicateHiderSettings(this);
        }

        #region Events       
        public override void OnApplicationStarted()
        {
            BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
            if (settings.UpdateAutomatically) 
                PlayniteApi.Database.Games.Update(SetDuplicateState(PlayniteApi.Database.Games, Hidden));
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            settings.OnSettingsChanged += Settings_OnSettingsChanged;
        }


        public override void OnApplicationStopped()
        {
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            settings.OnSettingsChanged -= Settings_OnSettingsChanged;
            SavePluginSettings(settings);
        }

        private void Settings_OnSettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings)
        {
            if (newSettings.UpdateAutomatically)
            {
                BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                PlayniteApi.Database.Games.Update(SetDuplicateState(PlayniteApi.Database.Games, Hidden));
            }
        }
        
        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            if (settings.UpdateAutomatically)
            {
                var filter = GetGameFilter();
                UpdateDuplicateState(e.AddedItems.Filter<IEnumerable<Game>, IFilter<IEnumerable<Game>>>(filter), Hidden);
                var nameFilter = GetNameFilter();
                foreach (var game in e.RemovedItems.Filter<IEnumerable<Game>, IFilter<IEnumerable<Game>>>(filter))
                {
                    var name = game.Name.Filter(nameFilter);
                    if (index.ContainsKey(name))
                    {
                        index[name].Remove(game.Id);
                        PlayniteApi.Database.Games.Update(SetDuplicateState(index[name], Hidden));
                    }
                }
            }
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
        }

        private void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            if (settings.UpdateAutomatically)
            {
                UpdateDuplicateState((from update in e.UpdatedItems select update.NewData).Filter(GetGameFilter()), Hidden);
            }
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
        }

        #endregion

        public override ISettings GetSettings(bool firstRunSettings)
        {
            if (firstRunSettings)
            {
                settings.Priorities = new List<string> {
                    "Steam",
                    "GOG",
                    "Epic",
                    "Amazon",
                    "Humble",
                    "Twitch",
                    "Xbox",
                    "Uplay",
                    "Origin",
                    "Battle.net",
                    "Rockstar Games",
                    "itch.io",
                    "Bethesda",
                    "Undefined"
                };
                settings.IncludePlatforms = new List<string> { "PC", "Undefined" };
            }
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

        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Hide Duplicates",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var hidden = SetDuplicateState(PlayniteApi.Database.Games, Hidden);
                        PlayniteApi.Database.Games.Update(hidden);
                        PlayniteApi.Dialogs.ShowMessage($"{hidden.Where(g => g.Hidden).Count()} games have been hidden.", "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                },
                new MainMenuItem
                {
                    Description = "Reveal Duplicates",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(PlayniteApi.Database.Games, Visible);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Dialogs.ShowMessage($"{revealed.Where(g => !g.Hidden).Count()} games have been revealed.", "DuplicateHider");
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                },
                new MainMenuItem
                {
                    Description = "Add Selected Games to Ignore List",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        foreach(var game in PlayniteApi.MainView.SelectedGames) settings.IgnoredGames.Add(game.Id);
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(PlayniteApi.Database.Games, Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                },
                new MainMenuItem
                {
                    Description = "Remove Selected Games from Ignore List",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        foreach(var game in PlayniteApi.MainView.SelectedGames) settings.IgnoredGames.Remove(game.Id);
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(PlayniteApi.Database.Games, Hidden);
                        PlayniteApi.Database.Games.Update(revealed);
                        PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
                    }
                }
            };
        }

        private void UpdateDuplicateState(IEnumerable<Game> games, Visibility visibility)
        {
            var nameFilter = GetNameFilter();
            foreach (var game in games)
            {
                var name = game.Name.Filter(nameFilter);
                if (index.ContainsKey(name))
                {
                    index[name].Remove(game.Id);
                }
                else
                {
                    index[name] = new List<Guid> { };
                }
                index[name].InsertSorted(game.Id, GetGamePriority);
            }
            PlayniteApi.Database.Games.Update(SetDuplicateState(games, visibility));
        }

        private IList<Game> SetDuplicateState(IEnumerable<Game> games, Visibility visibility)
        {
            List<Game> toUpdate = new List<Game> { };
            bool hidden = visibility == Hidden ? true : false;
            foreach (var copies in index.Values)
            {
                for(int i = 1; i < copies.Count; ++i)
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

        private IList<Game> SetDuplicateState(IEnumerable<Guid> gameId, Visibility visibility)
        {
            return SetDuplicateState(from id in gameId where (PlayniteApi.Database.Games.Get(id) != null) select PlayniteApi.Database.Games.Get(id), visibility);
        }

        private void BuildIndex(IEnumerable<Game> games, IFilter<IEnumerable<Game>> gameFilter, IFilter<string> nameFilter)
        {
            index.Clear();
            var filter = GetNameFilter();
            foreach (var game in games.Filter(gameFilter))
            {
                var cleanName = game.Name.Filter(nameFilter);
                if (!index.ContainsKey(cleanName))
                    index[cleanName] = new List<Guid> { };
                index[cleanName].InsertSorted(game.Id, GetGamePriority);
            }
        }

        private int GetGamePriority(Guid id)
        {
            var rankRange = settings.Priorities.Count;
            if (PlayniteApi.Database.Games.Get(id) is Game game)
            {
                return GetSourceRank(game) - (rankRange * (game.IsInstalled ? 1 : 0));
            }
            return rankRange;
        }

        private int GetSourceRank(Game game)
        {
            int index = settings.Priorities.IndexOf(game.GetSourceName());
            if (index > -1)
            {
                return index;
            } else
            {
                return settings.Priorities.Count;
            }
        }

        IFilter<string> GetNameFilter()
        {
            return IFilter<string>.MakeChain(
                new CaseFilter(CaseFilter.Case.Lower),
                new WhiteSpaceFilter(),
                new ReplaceFilter("and", "&", "+"),
                new ReplaceFilter("goty", "gameoftheyearedition", "gotyedition", "gameoftheyear"),
                new SpecialCharFilter()
            );
        }

        IFilter<IEnumerable<Game>> GetGameFilter()
        {
            return IFilter<IEnumerable<Game>>.MakeChain(
                new PlatformFilter(true, settings.IncludePlatforms),
                new SourceFilter(false, settings.ExcludeSources),
                new CategoryFilter(false, settings.ExcludeCategories),
                new IgnoreFilter(settings.IgnoredGames)
            );
        }
    }
}