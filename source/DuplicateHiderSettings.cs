using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DuplicateHider
{
    public class DuplicateHiderSettings : ISettings
    {
        private readonly DuplicateHider plugin;

        public delegate void SettingsChanged(DuplicateHiderSettings oldSettings, DuplicateHiderSettings newSettings);
        public event SettingsChanged OnSettingsChanged;

        [JsonIgnore]
        private DuplicateHiderSettings previousSettings = null;

        public bool UpdateAutomatically { get; set; } = false;
        public bool ShowOtherCopiesInGameMenu { get; set; } = false;
        public string DisplayString { get; set; } = "{Name} [{Installed} on {'Source'}{, ROM: 'ImageNameNoExt}]";

        public UniqueList<string> Priorities { get; set; } = new UniqueList<string>();
        [JsonIgnore]
        public Dictionary<Guid, Guid> SharedGameIds { get; set; } = new Dictionary<Guid, Guid>();


        public UniqueList<string> IncludePlatforms { get; set; } = new UniqueList<string> { "PC", Constants.UNDEFINED_SOURCE };
        public UniqueList<string> ExcludeSources { get; set; } = new UniqueList<string>();
        public UniqueList<string> ExcludeCategories { get; set; } = new UniqueList<string>();
        public HashSet<Guid> IgnoredGames { get; set; } = new HashSet<Guid>();

        public bool AddHiddenToIgnoreList { get; set; } = false;

        public List<ReplaceFilter> ReplaceFilters { get; set; } = new List<ReplaceFilter>();

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public DuplicateHiderSettings()
        {
        }

        public DuplicateHiderSettings(DuplicateHider plugin)
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


        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            previousSettings = this.Copy();
            plugin.SettingsView.Dispatcher.Invoke(() =>
            {
                plugin.SettingsView.AutoUpdateCheckBox.IsChecked = UpdateAutomatically;
                plugin.SettingsView.ShowCopiesInGameMenu.IsChecked = ShowOtherCopiesInGameMenu;
                plugin.SettingsView.AddHiddenToIgnoreList.IsChecked = AddHiddenToIgnoreList;

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
                        var item = new ListBoxItem();
                        item.Tag = id;
                        item.ContextMenu = new ContextMenu();
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

                    var installedItem = new MenuItem();
                    installedItem.Header = "Installed";
                    installedItem.Click += InsertVariable;
                    installedItem.Tag = "{'Installed'}";
                    contextMenu.Items.Add(installedItem);

                    var sourceItem = new MenuItem();
                    sourceItem.Header = "SourceName";
                    sourceItem.Click += InsertVariable;
                    sourceItem.Tag = "{'Source'}";
                    contextMenu.Items.Add(sourceItem);

                    foreach (var variable in typeof(ExpandableVariables).GetFields())
                    {
                        var item = new MenuItem();
                        item.Header = variable.Name;
                        item.Click += InsertVariable;
                        item.Tag = ((string)variable.GetRawConstantValue()).Replace("{", "{'").Replace("}", "'}");
                        contextMenu.Items.Add(item);
                    }
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
        }

        public void EndEdit()
        {
            // Apply changed settings
            plugin.SettingsView.AutoUpdateCheckBox.Dispatcher.Invoke(() =>
            {
                {
                    UpdateAutomatically = plugin.SettingsView.AutoUpdateCheckBox.IsChecked ?? false;
                    ShowOtherCopiesInGameMenu = plugin.SettingsView.ShowCopiesInGameMenu.IsChecked ?? false;
                    AddHiddenToIgnoreList = plugin.SettingsView.AddHiddenToIgnoreList.IsChecked ?? false;
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
            });

            OnSettingsChanged?.Invoke(previousSettings, this);
            plugin.SavePluginSettings(this);
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