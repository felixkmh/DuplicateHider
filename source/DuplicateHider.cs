using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

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
            if (settings.UpdateAutomatically)
            {
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
            }

            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged += Games_ItemCollectionChanged;
            settings.OnSettingsChanged += Settings_OnSettingsChanged;

            playButtonExt.Checked += (_,__) => { 
                playButtonExt.Content = "-";
                var others = GetOtherCopies(PlayniteApi.MainView.SelectedGames.FirstOrDefault());
                playButtonExt.Visibility = others.Count < 1 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                otherCopiesPanel.Children.Clear();
                othersCount = others.Count;

                foreach (Game game in others)
                {
                    var display = new StackPanel() { Orientation = Orientation.Horizontal };
                    TextBlock text = new TextBlock() { Text = ExpandDisplayString(game, settings.DisplayString), Opacity = game.IsInstalled ? 1 : 0.7 };
                    display.Children.Add(text);
                    Button element = new Button() { Content = display, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 2) };
                    element.Click += (___, ____) => { PlayniteApi.StartGame(game.Id); };
                    otherCopiesPanel.Children.Add(element);
                }
            };
            playButtonExt.Unchecked += (_, __) => {
                playButtonExt.Content = $"+{(othersCount == 1 ? "" : othersCount.ToString())}";
            };

            playButtonExtPopup.SetBinding(Popup.IsOpenProperty, new Binding("IsChecked") { Mode = BindingMode.TwoWay, Delay = 200, Source = playButtonExt });
            playButtonExtPopup.PlacementTarget = playButtonExt;
            playButtonExt.Padding = new Thickness(0);
            playButtonExt.HorizontalContentAlignment = HorizontalAlignment.Center;
            playButtonExt.VerticalContentAlignment = VerticalAlignment.Center;
            playButtonExtPopup.Placement = PlacementMode.Bottom;
            playButtonExtPopup.Child = new ScrollViewer() { Content = otherCopiesPanel, MaxHeight = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Auto};
            playButtonExtPopup.StaysOpen = false;
            otherCopiesPanel.Background = Brushes.Transparent;
        }


        public override void OnApplicationStopped()
        {
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            settings.OnSettingsChanged -= Settings_OnSettingsChanged;
            SavePluginSettings(settings);
        }

        Button playButton = null;
        ToggleButton playButtonExt = new ToggleButton() { Content = "+" };
        Popup playButtonExtPopup = new Popup();
        StackPanel otherCopiesPanel = new StackPanel();
        int othersCount = 0;
        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            if (settings.EnableUiIntegration && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (playButton == null)
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        var op = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action( () => {
                            var detailsView = UiIntegration.FindVisualChildren(Application.Current.MainWindow, "PART_ViewDetails").FirstOrDefault();
                            if (detailsView != null)
                            {
                                playButton = UiIntegration.FindVisualChildren<Button>(detailsView, "PART_ButtonContextAction").FirstOrDefault();
                            }
                        }));
                        op.Completed += Op_Completed;
                    });
                }
                if (index.Count == 0 && settings.UpdateAutomatically)
                {
                    BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                }
                var others = GetOtherCopies(args.NewValue.FirstOrDefault());
                playButtonExt.Visibility = others.Count < 1 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                othersCount = others.Count;
                playButtonExt.Content = $"+{(others.Count == 1 ? "" : others.Count.ToString())}";
            } else
            {
                playButtonExt.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Op_Completed(object sender, EventArgs e)
        {
            if (playButton is Button)
            {
                playButtonExtPopup.HorizontalOffset = -playButton.Width -playButton.Margin.Right;
                playButtonExtPopup.VerticalOffset = 2;
                if (playButton.Parent is Panel panel)
                {
                    if (playButtonExt.Parent is Panel oldPanel)
                    {
                        oldPanel.Children.Remove(playButtonExt);
                    }

                    playButtonExt.Width = playButton.Height;
                    playButtonExt.Height = playButton.Height;
                    otherCopiesPanel.MinWidth = playButton.Width + playButton.Margin.Right + playButtonExt.Width;

                    var playButtonIdx = panel.Children.IndexOf(playButton);
                    panel.Dispatcher.Invoke(() => { 
                        panel.Children.Insert(playButtonIdx + 1, playButtonExt);
                    });

                }
            }
        }

        private void Settings_OnSettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings)
        {
            if (newSettings.UpdateAutomatically)
            {
                PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                PlayniteApi.Database.Games.Update(SetDuplicateState(Visible));
                gameFilters = null;
                nameFilters = null;
                BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
                PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
            }
        }

        private void Games_ItemCollectionChanged(object sender, ItemCollectionChangedEventArgs<Game> e)
        {
            PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
            PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
            var toUpdate = new List<Game>();
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
                        if (index[name].Remove(game.Id) && index[name].Count == 1)
                        {
                            if (PlayniteApi.Database.Games.Get(index[name][0]) is Game last)
                            {
                                if (last.Hidden)
                                {
                                    last.Hidden = false;
                                    toUpdate.Add(last);
                                }
                            }
                            else
                            {
                                index[name] = null;
                            }
                        }
                    }
                }
            }
            PlayniteApi.Database.Games.Update(toUpdate);
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
                foreach (var change in e.UpdatedItems)
                {
                    if (change.OldData.Hidden != change.NewData.Hidden)
                    {
                        settings.IgnoredGames.Add(change.NewData.Id);
                    }
                }
                BuildIndex(PlayniteApi.Database.Games, gameFilter, nameFilter);
                var revealed = SetDuplicateState(Hidden);
                PlayniteApi.Database.Games.Update(revealed);
                foreach (var change in e.UpdatedItems)
                {
                    if (change.OldData.Hidden != change.NewData.Hidden)
                    {
                        if (index.TryGetValue(nameFilter.Apply(change.OldData.Name), out var copies)) {
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
            if (settings.UpdateAutomatically)
            {
                foreach (var oldData in (from update in e.UpdatedItems select update.OldData).Filter(gameFilter))
                {
                    var filteredName = oldData.Name.Filter(nameFilter);
                    if (index.TryGetValue(filteredName, out var guids))
                    {
                        if (guids.Remove(oldData.Id))
                        {
                            if (guids.Count == 1)
                            {
                                if (PlayniteApi.Database.Games.Get(guids[0]) is Game game)
                                {
                                    game.Hidden = false;
                                    PlayniteApi.Database.Games.Update(game);
                                }
                            }
                        }
                    }
                }
                foreach (var newData in (from update in e.UpdatedItems select update.NewData).Filter(gameFilter))
                {
                    var filteredName = newData.Name.Filter(nameFilter);
                    if (index.TryGetValue(filteredName, out var guids))
                    {
                        guids.InsertSorted(newData.Id, GetGamePriority);
                    }
                }
                PlayniteApi.Database.Games.Update(SetDuplicateState(Hidden));
            }
            PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;
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

        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
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
                        foreach (var group in index)
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
                    Description = "Hide Duplicates",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
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
                new MainMenuItem
                {
                    Description = "Reveal Duplicates",
                    MenuSection = "@|DuplicateHider",
                    Action = (context) => {
                        PlayniteApi.Database.Games.ItemUpdated -= Games_ItemUpdated;
                        PlayniteApi.Database.Games.ItemCollectionChanged -= Games_ItemCollectionChanged;
                        BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Visible);
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
                        foreach(var game in PlayniteApi.MainView.SelectedGames) { settings.IgnoredGames.Add(game.Id); } BuildIndex(PlayniteApi.Database.Games, GetGameFilter(), GetNameFilter());
                        var revealed = SetDuplicateState(Hidden);
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
                        foreach(var group in index.OrderBy(g => g.Key).Where(g => g.Value.Count > 1))
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
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {

            var entries = new List<GameMenuItem>();
            if (settings.ShowOtherCopiesInGameMenu)
            {
                if (args.Games.Count == 1)
                {
                    var selected = args.Games[0];
                    var others = GetOtherCopies(selected);
                    foreach (var copy in others)
                    {
                        entries.Add(new GameMenuItem
                        {
                            Action = context => PlayniteApi.StartGame(copy.Id),
                            MenuSection = $"Other Copies: {others.Count()}",
                            Description = ExpandDisplayString(copy, settings.DisplayString)
                        });
                    }
                }
            }

            return entries;
        }

        private List<Game> GetOtherCopies(Game game)
        {
            if (game == null) return new List<Game>();
            var name = game.Name.Filter(GetNameFilter());
            var duplicates = new List<Game>();
            if (index.TryGetValue(name, out var copies))
            {
                var others = copies.Where(c => c != game.Id);
                foreach (var copyId in others)
                {
                    if (PlayniteApi.Database.Games.Get(copyId) is Game copy)
                    {
                        duplicates.Add(copy);
                    }
                }
            }
            return duplicates
                .OrderByDescending(g => g.IsInstalled)
                .ThenBy(g => GetGamePriority(g.Id))
                .ThenBy(g => g.Name)
                .ThenBy(g => g.GetSourceName())
                .ToList();
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
            PlayniteApi.Database.Games.Update(SetDuplicateState(visibility));
        }

        private IList<Game> SetDuplicateState(Visibility visibility)
        {
            List<Game> toUpdate = new List<Game> { };
            bool hidden = visibility == Hidden ? true : false;
            foreach (var copies in index.Values)
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
            index.Clear();
            foreach (var game in games.Filter(gameFilter))
            {
                var cleanName = game.Name.Filter(nameFilter);
                if (!index.ContainsKey(cleanName))
                {
                    index[cleanName] = new List<Guid> { };
                }

                index[cleanName].InsertSorted(game.Id, GetGamePriority);
            }
        }

        static readonly Regex regexVariable = new Regex(@"{(?:(?<Prefix>[^'{}]*)')?(?<Variable>[^'{}]+)(?:'(?<Suffix>[^'{}]*))?}");
        static readonly int prefixIdx = regexVariable.GroupNumberFromName("Prefix");
        static readonly int suffixIdx = regexVariable.GroupNumberFromName("Suffix");
        static readonly int variableIdx = regexVariable.GroupNumberFromName("Variable");

        public static IList<KeyValuePair<string, string>> GetGameVariables()
        {
            var vars = new List<KeyValuePair<string, string>>();

            vars.Add(new KeyValuePair<string, string> ("Installed", "{'Installed'}"));
            vars.Add(new KeyValuePair<string, string>("SourceName", "{'SourceName'}"));


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
            }
            else
            {
                return settings.Priorities.Count;
            }
        }

        IFilter<string> nameFilters = null;

        IFilter<string> GetNameFilter()
        {
            if (nameFilters == null)
            {
                var customRules = IFilter<string>.MakeChain(settings.ReplaceFilters.Cast<IFilter<string>>().ToList());
                nameFilters = customRules.Append(
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
            return nameFilters;
        }

        IFilter<IEnumerable<Game>> gameFilters = null;

        IFilter<IEnumerable<Game>> GetGameFilter()
        {
            if (gameFilters == null)
            {
                gameFilters = IFilter<IEnumerable<Game>>.MakeChain(
                    new PlatformFilter(true, settings.IncludePlatforms),
                    new SourceFilter(false, settings.ExcludeSources),
                    new CategoryFilter(false, settings.ExcludeCategories),
                    new IgnoreFilter(settings.IgnoredGames)
                );
            }
            return gameFilters;
        }
    }
}