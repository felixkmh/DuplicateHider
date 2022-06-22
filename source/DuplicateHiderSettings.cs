using DuplicateHider.Data;
using DuplicateHider.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace DuplicateHider
{
    public class DuplicateHiderSettings : ISettings
    {
        private readonly DuplicateHiderPlugin plugin;

        public delegate void SettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings);
        public event SettingsChanged OnSettingsChanged;

        [JsonIgnore]
        private DuplicateHiderSettings previousSettings = null;
        [QuickSearch.Attributes.GenericOption("LOC_DH_AutomaticUpdate", Description = "LOC_DH_AutomaticUpdateTooltip")]
        public bool UpdateAutomatically { get; set; } = false;
        [QuickSearch.Attributes.GenericOption("LOC_DH_ShowOtherCopies", Description = "LOC_DH_ShowOtherCopiesTooltip")]
        public bool ShowOtherCopiesInGameMenu { get; set; } = false;
        public string DisplayString { get; set; } = "{Name} [{Installed} on {'Source'}{, ROM: 'ImageNameNoExt}]";

        public UniqueList<string> Priorities { get; set; } = new UniqueList<string>();
        [JsonIgnore]
        public Dictionary<Guid, Guid> SharedGameIds { get; set; } = new Dictionary<Guid, Guid>();

        public EnabledFieldsModel DefaultEnabledFields { get; set; } = new EnabledFieldsModel();
        public UniqueList<string> IncludePlatforms { get; set; } = new UniqueList<string> { "PC (Windows)", Constants.UNDEFINED_SOURCE };
        public UniqueList<string> ExcludeSources { get; set; } = new UniqueList<string>();
        public UniqueList<string> ExcludeCategories { get; set; } = new UniqueList<string>();
        public HashSet<Guid> IgnoredGames { get; set; } = new HashSet<Guid>();
        [QuickSearch.Attributes.GenericOption("LOC_DH_IgnoreManuallyHiddenGames", Description = "LOC_DH_IgnoreManuallyHiddenGamesTooltip")]
        public bool AddHiddenToIgnoreList { get; set; } = false;
        [QuickSearch.Attributes.GenericOption("LOC_DH_PreferUserIcons", Description = "LOC_DH_PreferUserIconsTooltip")]
        public bool PreferUserIcons { get; set; } = true;
        [QuickSearch.Attributes.GenericOption("LOC_DH_EnableThemeIcons", Description = "LOC_DH_EnableThemeIconsTooltip")]
        public bool EnableThemeIcons { get; set; } = true;
        [QuickSearch.Attributes.GenericOption("LOC_DH_EnableUIIntegration", Description = "LOC_DH_EnableUIIntegrationTooltip")]
        public bool EnableUiIntegration { get; set; } = true;
        [QuickSearch.Attributes.GenericOption("LOC_DH_ShowSingleIcon", Description = "LOC_DH_ShowSingleIconTooltip")]
        public bool ShowSingleIcon { get; set; } = false;
        [QuickSearch.Attributes.GenericOption("LOC_DH_SuppressNotification", Description = "LOC_DH_SuppressNotificationTooltip")]
        public bool SupressThemeIconNotification { get; set; } = false;
        [QuickSearch.Attributes.GenericOption("LOC_DH_PrioritizeNewerGames")]
        public bool PreferNewerGame { get; set; } = true;

        public List<ReplaceFilter> ReplaceFilters { get; set; } = new List<ReplaceFilter>();

        public ObservableCollection<PriorityProperty> PriorityProperties { get; set; } = null;

        public List<CustomGroup> CustomGroups {get; set;} = new List<CustomGroup>();

        [JsonIgnore]
        public ICommand AddPriorityPropertyCommand { get; private set; }

        [JsonIgnore]
        public ICommand RemovePriorityPropertyCommand { get; private set; }

        [JsonIgnore]
        public IEnumerable<string> AvailablePriorityProperties
            => typeof(Game).GetProperties()
            .Where(p => Converters.GamePropertyToNameConverter.Instance.Convert(p.Name, typeof(string), null, null) != null)
            .Select(p => p.Name)
            .OrderBy(p => Converters.GamePropertyToNameConverter.Instance.Convert(p, typeof(string), null, null));

        // Tag Ids
        [JsonIgnore]
        private Guid hiddenTagId = Guid.Empty;
        [JsonIgnore]
        private Guid revealedTagId = Guid.Empty;
        [JsonIgnore]
        private Guid highPrioTagId = Guid.Empty;
        [JsonIgnore]
        private Guid lowPrioTagId = Guid.Empty;
        public Guid HiddenTagId
        {
            get
            {
                if (plugin != null && hiddenTagId == Guid.Empty)
                {
                    hiddenTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Hidden").Id;
                } else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(hiddenTagId) == null)
                {
                    hiddenTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Hidden").Id;
                }
                return hiddenTagId;
            }
            set => hiddenTagId = value;
        }
        public Guid RevealedTagId
        {
            get
            {
                if (plugin != null && revealedTagId == Guid.Empty)
                {
                    revealedTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Revealed").Id;
                }
                else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(revealedTagId) == null)
                {
                    revealedTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Revealed").Id;
                }
                return revealedTagId;
            }
            set => revealedTagId = value;
        }
        public Guid HighPrioTagId
        {
            get
            {
                if (plugin != null && highPrioTagId == Guid.Empty)
                {
                    highPrioTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] High").Id;
                }
                else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(highPrioTagId) == null)
                {
                    highPrioTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] High").Id;
                }
                return highPrioTagId;
            }
            set => highPrioTagId = value;
        }
        public Guid LowPrioTagId
        {
            get
            {
                if (plugin != null && lowPrioTagId == Guid.Empty)
                {
                    lowPrioTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Low").Id;
                }
                else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(lowPrioTagId) == null)
                {
                    lowPrioTagId = plugin.PlayniteApi.Database.Tags.Add("[DH] Low").Id;
                }
                return lowPrioTagId;
            }
            set => lowPrioTagId = value;
        }

        public ListBoxItem CreateReplacementFilterItem(ReplaceFilter filter = null)
        {
            var item = new ListBoxItem();
            if (filter is null)
            {
                item.Tag = "empty";
            }
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            var left = new TextBox
            {
                Tag = "left"
            };
            left.TextChanged += FilterTextChanged;
            left.Text = string.Empty;
            if (filter is ReplaceFilter rf)
            {
                left.Text = filter.asRegex? filter.regex.ToString() : Regex.Unescape(filter.regex.ToString());
            }
            var right = new TextBox
            {
                Tag = "right"
            };
            right.TextChanged += FilterTextChanged;
            right.Text = filter is null ? string.Empty : filter._replace;
            var arrow = new Label
            {
                Content = "→"
            };
            sp.Children.Add(left);
            sp.Children.Add(arrow);
            sp.Children.Add(right);

            var cb = new CheckBox
            {
                IsChecked = filter?.asRegex ?? false,
                FlowDirection = FlowDirection.RightToLeft
            };
            var hl = new Hyperlink(new Run("Regex"));
            var tb = new TextBlock();
            var margin = cb.Margin;
            margin.Left = 5;
            cb.Margin = margin;

            var pretext = new Run("");
            hl.NavigateUri = new Uri("https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions");
            hl.Click += Hl_Click;
            tb.FlowDirection = FlowDirection.LeftToRight;
            tb.Inlines.Add(pretext);
            tb.Inlines.Add(hl);
            cb.Content = tb;
            sp.Children.Add(cb);

            item.Content = sp;
            if (filter != null)
            {
                var bt = new Button
                {
                    Content = "X",
                    Tag = item,
                };
                bt.Click += DeleteReplaceFilterClick;
                sp.Children.Insert(0, bt);
            }
            return item;
        }

        private void Hl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink hl)
            {
                System.Diagnostics.Process.Start(hl.NavigateUri.AbsoluteUri);
            }
        }

        private void DeleteReplaceFilterClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                plugin.SettingsView.ReplacementRulesListBox.Items.Remove(bt.Tag);
            }
        }

        public static ReplaceFilter CreateFilterFromListBoxItem(ListBoxItem item)
        {
            if (item.Tag as string == "empty")
            {
                return null;
            }
            if (item.Content is StackPanel sp )
            {
                var isRegex = false;
                if (sp.Children.OfType<CheckBox>().FirstOrDefault() is CheckBox cb)
                {
                    isRegex = cb.IsChecked ?? false;
                }
                var textboxes = sp.Children.OfType<TextBox>();
                TextBox left = textboxes.First(b => b.Tag as string == "left");
                TextBox right = textboxes.First(b => b.Tag as string == "right");
                if (left.Text.Length > 0)
                {
                    if (isRegex)
                    {
                        return new ReplaceFilter(right.Text, new Regex(left.Text, RegexOptions.IgnoreCase)) { asRegex = true };
                    } else
                    {
                        return new ReplaceFilter(right.Text, new Regex(Regex.Escape(left.Text), RegexOptions.IgnoreCase));
                    }
                }
            }
            return null;
        }

        private void FilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.Parent is StackPanel sp)
                {
                    if (sp.Parent is ListBoxItem item)
                    {
                        var textboxes = sp.Children.OfType<TextBox>();
                        TextBox left = textboxes.First(b => b.Tag as string == "left");
                        TextBox right = textboxes.First(b => b.Tag as string == "right");
                        if (left.Text.Length > 0 || right.Text.Length > 0)
                        {
                            if (item.Tag as string == "empty")
                            {
                                var list = item.Parent as ListBox;
                                item.Tag = null;
                                list.Items.Dispatcher.Invoke(() =>
                                {
                                    var bt = new Button
                                    {
                                        Content = "X",
                                        Tag = item
                                    };
                                    bt.Click += DeleteReplaceFilterClick;
                                    sp.Children.Insert(0, bt);
                                    list.Items.Add(CreateReplacementFilterItem());
                                });
                            }
                        }
                        if (left.Text.Length == 0 && right.Text.Length == 0)
                        {
                            if (item.Tag as string != "empty")
                            {
                                item.Tag = null;
                                var list = item.Parent as ListBox;
                                list.Items.Dispatcher.Invoke(() =>
                                {
                                    list.Items.Remove(item);
                                });
                            }
                        }
                    }
                }
            }
        }

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public DuplicateHiderSettings()
        {
        }

        public DuplicateHiderSettings(DuplicateHiderPlugin plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<DuplicateHiderSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Priorities = savedSettings.Priorities;
                UpdateAutomatically = savedSettings.UpdateAutomatically;
                IncludePlatforms = savedSettings.IncludePlatforms;
                ExcludeCategories = savedSettings.ExcludeCategories;
                ExcludeSources = savedSettings.ExcludeSources;
                IgnoredGames = savedSettings.IgnoredGames;
                ShowOtherCopiesInGameMenu = savedSettings.ShowOtherCopiesInGameMenu;
                DisplayString = savedSettings.DisplayString;
                AddHiddenToIgnoreList = savedSettings.AddHiddenToIgnoreList;
                ReplaceFilters = savedSettings.ReplaceFilters;
                PreferUserIcons = savedSettings.PreferUserIcons;
                EnableThemeIcons = savedSettings.EnableThemeIcons;
                EnableUiIntegration = savedSettings.EnableUiIntegration;
                ShowSingleIcon = savedSettings.ShowSingleIcon;
                SupressThemeIconNotification = savedSettings.SupressThemeIconNotification;
                CustomGroups = savedSettings.CustomGroups;
                HiddenTagId = savedSettings.HiddenTagId;
                RevealedTagId = savedSettings.RevealedTagId;
                DefaultEnabledFields = savedSettings.DefaultEnabledFields;
                PreferNewerGame = savedSettings.PreferNewerGame;
                PriorityProperties = savedSettings.PriorityProperties;
            }

            if (Priorities.Count == 0)
            {
                Priorities = new UniqueList<string>
                {
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
                    Constants.UNDEFINED_SOURCE
                };
            }

            AddPriorityPropertyCommand = new RelayCommand<string>(
                s => PriorityProperties.Add(new PriorityProperty(s, plugin.PlayniteApi)),
                s => !PriorityProperties.Any(p => p.PropertyName == s)
            );

            RemovePriorityPropertyCommand = new RelayCommand<PriorityProperty>(
                p => PriorityProperties.Remove(p)
            );
        }

        public void BeginEdit()
        {

            // Code executed when settings view is opened and user starts editing values.
            previousSettings = this.Copy();
            plugin.SettingsView.Dispatcher.Invoke(() =>
            {
                plugin.SettingsView.GroupsList.DataContext = new CustomGroupsViewModel(CustomGroups) { Synchronize = false };

                plugin.SettingsView.AutoUpdateCheckBox.IsChecked = UpdateAutomatically;
                plugin.SettingsView.ShowCopiesInGameMenu.IsChecked = ShowOtherCopiesInGameMenu;
                plugin.SettingsView.AddHiddenToIgnoreList.IsChecked = AddHiddenToIgnoreList;
                plugin.SettingsView.PrioritizeNewerGame.IsChecked = PreferNewerGame;

                // Populate Replacement Rules
                {
                    foreach (var filter in ReplaceFilters)
                    {
                        if (filter is ReplaceFilter rf)
                        {
                            plugin.SettingsView.ReplacementRulesListBox.Items.Add(
                                CreateReplacementFilterItem(rf)
                            );
                        }
                    }
                    plugin.SettingsView.ReplacementRulesListBox.Items.Add(
                        CreateReplacementFilterItem()
                    );
                }

                // Populate Priority list
                {
                    plugin.SettingsView.PriorityListBox.Items.Clear();
                    // Add missing sources to the end of the Priority list
                    foreach (var source in plugin.PlayniteApi.Database.Sources)
                    {
                        Priorities.AddMissing(source.Name);
                    }
                    Priorities.AddMissing(Constants.UNDEFINED_SOURCE);

                    // Add valid entries to the PriorityListBox
                    foreach (var sourceName in Priorities)
                    {
                        if (plugin.PlayniteApi.Database.Sources.TryFind(s => s.Name == sourceName, out var source))
                        {
                            plugin.SettingsView.PriorityListBox.Items.Add(plugin.SettingsView.CreatePriorityEntry(source));
                        }
                        else if (sourceName == Constants.UNDEFINED_SOURCE)
                        {
                            plugin.SettingsView.PriorityListBox.Items.Add(plugin.SettingsView.CreatePriorityEntry(null));
                        }
                    }
                }

                // Populate PlatformComboBox
                {
                    List<CheckBox> checkBoxes = new List<CheckBox>();
                    foreach (var platform in plugin.PlayniteApi.Database.Platforms.Concat(new List<Platform> { null }))
                    {
                        string platformName = platform != null ? platform.Name : Constants.UNDEFINED_PLATFORM;
                        var cb = new CheckBox { Content = platformName, Tag = platform };
                        cb.IsChecked = IncludePlatforms.Contains(platformName);
                        checkBoxes.Add(cb);
                    }
                    checkBoxes = checkBoxes.OrderByDescending(cb => cb.IsChecked).ThenBy(cb => cb.Content).ToList();
                    plugin.SettingsView.Platforms = checkBoxes;
                    checkBoxes.ForEach(cb => plugin.SettingsView.PlatformComboBox.Items.Add(cb));
                }

                // Populate SourceComboBox
                {
                    List<CheckBox> checkBoxes = new List<CheckBox>();
                    foreach (var source in plugin.PlayniteApi.Database.Sources.Concat(new List<GameSource> { null }))
                    {
                        string sourceName = source != null ? source.Name : Constants.UNDEFINED_SOURCE;
                        var cb = new CheckBox { Content = sourceName, Tag = source };
                        cb.IsChecked = ExcludeSources.Contains(sourceName);
                        checkBoxes.Add(cb);
                    }
                    checkBoxes = checkBoxes.OrderByDescending(cb => cb.IsChecked).ThenBy(cb => cb.Content).ToList();
                    plugin.SettingsView.Sources = checkBoxes;
                    checkBoxes.ForEach(cb => plugin.SettingsView.SourceComboBox.Items.Add(cb));
                }

                // Populate CategoriesComboBox
                {
                    List<CheckBox> checkBoxes = new List<CheckBox>();
                    foreach (var category in plugin.PlayniteApi.Database.Categories)
                    {
                        var cb = new CheckBox { Content = category.Name, Tag = category };
                        cb.IsChecked = ExcludeCategories.Contains(category.Name);
                        checkBoxes.Add(cb);
                    }
                    checkBoxes = checkBoxes.OrderByDescending(cb => cb.IsChecked).ThenBy(cb => cb.Content).ToList();
                    plugin.SettingsView.Categories = checkBoxes;
                    checkBoxes.ForEach(cb => plugin.SettingsView.CategoriesComboBox.Items.Add(cb));
                }

                // Populate IgnoredGames ListBox
                {
                    plugin.SettingsView.IgnoreListBox.Items.Clear();
                    foreach (var id in IgnoredGames)
                    {
                        var item = new ListBoxItem
                        {
                            Tag = id,
                            ContextMenu = new ContextMenu()
                        };
                        var menuItem = new MenuItem { Header = "Remove Entry", Tag = id };
                        menuItem.Click += RemoveIgnored_Click;
                        item.ContextMenu.Items.Add(menuItem);
                        var game = plugin.PlayniteApi.Database.Games.Get(id);
                        item.Content = game == null ? "Game not found: " + id.ToString() : $"{game.Name} ({game.GetSourceName()})";
                        item.ToolTip = item.Content;
                        plugin.SettingsView.IgnoreListBox.Items.Add(item);
                    }
                }

                // Add context menu options to FormatString TextField
                {
                    var textBox = plugin.SettingsView.DisplayStringTextBox;
                    textBox.Text = DisplayString ?? "";
                    var contextMenu = textBox.ContextMenu = new ContextMenu();

                    foreach (var variable in DuplicateHiderPlugin.GetGameVariables())
                    {
                        var item = new MenuItem
                        {
                            Header = variable.Key
                        };
                        item.Click += InsertVariable;
                        item.Tag = variable.Value;
                        contextMenu.Items.Add(item);
                    }
                }
                // Populate UI Integration Settings
                {
                    plugin.SettingsView.EnableThemeIconsChechBox.IsChecked = EnableThemeIcons;
                    plugin.SettingsView.UiIntegrationCheckBox.IsChecked = EnableUiIntegration;
                    plugin.SettingsView.PreferUserIconsCheckBox.IsChecked = PreferUserIcons;
                    plugin.SettingsView.ShowSingleSourceIconCheckBox.IsChecked = ShowSingleIcon;
                    plugin.SettingsView.OpenUserIconFolderButton.Click += (_, __) => System.Diagnostics.Process.Start("explorer.exe", plugin.GetUserIconFolderPath());
                    plugin.SettingsView.SuppressNotificationCheckBox.IsChecked = SupressThemeIconNotification;
                }
            });
        }

        private void InsertVariable(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item.Tag is string variable)
                {
                    var index = plugin.SettingsView.DisplayStringTextBox.CaretIndex;
                    plugin.SettingsView.DisplayStringTextBox.SelectedText = variable;
                    plugin.SettingsView.DisplayStringTextBox.CaretIndex = index + variable.Length;
                }
            }
        }

        private void RemoveIgnored_Click(object sender, RoutedEventArgs e)
        {
            List<Guid> toRemoveId = new List<Guid>();
            plugin.SettingsView.IgnoreListBox.Dispatcher.Invoke(() =>
            {
                var menuItem = sender as MenuItem;
                List<ListBoxItem> toRemove = new List<ListBoxItem>();
                foreach (ListBoxItem selected in plugin.SettingsView.IgnoreListBox.SelectedItems)
                {
                    toRemove.Add(selected);
                }
                foreach (var item in toRemove)
                {
                    item.Dispatcher.Invoke(() => toRemoveId.Add((Guid)item.Tag));
                }
                foreach (ListBoxItem item in toRemove)
                {
                    plugin.SettingsView.IgnoreListBox.Items.Remove(item);
                }
            });
            // foreach (var id in toRemoveId) IgnoredGames.Remove(id);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            PriorityProperties = previousSettings.PriorityProperties;
        }

        public void EndEdit()
        {
            bool requireRestart = false;
            // Apply changed settings
            plugin.SettingsView.AutoUpdateCheckBox.Dispatcher.Invoke(() =>
            {
                {
                    if (plugin.SettingsView.GroupsList.DataContext is CustomGroupsViewModel groups)
                    {
                        CustomGroups = groups.Groups.Select(g => { g.UpdateGroup(); return g.Group; }).ToList();
                    }

                    UpdateAutomatically = plugin.SettingsView.AutoUpdateCheckBox.IsChecked ?? UpdateAutomatically;
                    ShowOtherCopiesInGameMenu = plugin.SettingsView.ShowCopiesInGameMenu.IsChecked ?? ShowOtherCopiesInGameMenu;
                    AddHiddenToIgnoreList = plugin.SettingsView.AddHiddenToIgnoreList.IsChecked ?? AddHiddenToIgnoreList;
                    PreferNewerGame = plugin.SettingsView.PrioritizeNewerGame.IsChecked ?? true;
                }

                // Retrieve Replacement Rules
                {
                    ReplaceFilters.Clear();
                    foreach (var item in plugin.SettingsView.ReplacementRulesListBox.Items)
                    {
                        if (item is ListBoxItem lbi)
                        {
                            var rule = CreateFilterFromListBoxItem(lbi);
                            if (!(rule is null))
                            {
                                ReplaceFilters.Add(rule);
                            }
                        }
                    }
                }

                UniqueList<string> updatedPriorites = new UniqueList<string> { };
                {
                    foreach (ListBoxItem item in plugin.SettingsView.PriorityListBox.Items)
                    {
                        if (item.Tag is GameSource source)
                        {
                            updatedPriorites.AddMissing(source.Name);
                        }
                        else
                        {
                            updatedPriorites.AddMissing(Constants.UNDEFINED_SOURCE);
                        }
                    }
                }
                Priorities = updatedPriorites;
                {
                    foreach (CheckBox cb in plugin.SettingsView.PlatformComboBox.Items)
                    {
                        string name = cb.Content as string;
                        if (cb.IsChecked ?? false)
                        {
                            IncludePlatforms.AddMissing(name);
                        }
                        else
                        {
                            IncludePlatforms.Remove(name);
                        }

                    }
                }
                {
                    foreach (CheckBox cb in plugin.SettingsView.SourceComboBox.Items)
                    {
                        string name = cb.Content as string;
                        if (cb.IsChecked ?? false)
                        {
                            ExcludeSources.AddMissing(name);
                        }
                        else
                        {
                            ExcludeSources.Remove(name);
                        }

                    }
                }
                {
                    foreach (CheckBox cb in plugin.SettingsView.CategoriesComboBox.Items)
                    {
                        string name = cb.Content as string;
                        if (cb.IsChecked ?? false)
                        {
                            ExcludeCategories.AddMissing(name);
                        }
                        else
                        {
                            ExcludeCategories.Remove(name);
                        }

                    }
                }
                List<Guid> toIgnoreIds = new List<Guid>();
                IgnoredGames.Clear();
                {
                    List<ListBoxItem> toIgnore = new List<ListBoxItem>();
                    foreach (ListBoxItem item in plugin.SettingsView.IgnoreListBox.Items)
                    {
                        toIgnore.AddMissing(item);
                    }
                    foreach (var item in toIgnore)
                    {
                        item.Dispatcher.Invoke(() => toIgnoreIds.Add((Guid)item.Tag));
                    }
                    foreach (var id in toIgnoreIds)
                    {
                        IgnoredGames.Add(id);
                    }
                }

                {
                    DisplayString = plugin.SettingsView.DisplayStringTextBox.Text ?? DisplayString;
                }

                // retrieve UI Integration Settings
                {
                    bool prev = EnableUiIntegration;

                    EnableThemeIcons = plugin.SettingsView.EnableThemeIconsChechBox.IsChecked ?? EnableThemeIcons;
                    EnableUiIntegration = plugin.SettingsView.UiIntegrationCheckBox.IsChecked ?? EnableUiIntegration;
                    PreferUserIcons = plugin.SettingsView.PreferUserIconsCheckBox.IsChecked ?? PreferUserIcons;
                    ShowSingleIcon = plugin.SettingsView.ShowSingleSourceIconCheckBox.IsChecked ?? ShowSingleIcon;
                    SupressThemeIconNotification = plugin.SettingsView.SuppressNotificationCheckBox.IsChecked ?? SupressThemeIconNotification;
                    if (prev != EnableUiIntegration) requireRestart = true;
                }
            });

            OnSettingsChanged?.Invoke(previousSettings, this);
            plugin.SavePluginSettings(this);
            if (requireRestart)
                plugin.PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOC_DH_RestartMessage"), "DuplicateHider");
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}