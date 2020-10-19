using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public List<string> Priorities { get; set; } = new List<string>();

        public List<string> IncludePlatforms { get; set; } = new List<string>();
        public List<string> ExcludeSources { get; set; } = new List<string>();
        public List<string> ExcludeCategories { get; set; } = new List<string>();
        public HashSet<Guid> IgnoredGames { get; set; } = new HashSet<Guid>();

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
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            previousSettings = this.Copy();
            plugin.SettingsView.AutoUpdateCheckBox.Dispatcher.Invoke(() =>
            {
                plugin.SettingsView.AutoUpdateCheckBox.IsChecked = UpdateAutomatically;
            });
            plugin.SettingsView.PriorityListBox.Items.Dispatcher.Invoke(() =>
            {
                plugin.SettingsView.PriorityListBox.Items.Clear();
                foreach (var source in plugin.PlayniteApi.Database.Sources)
                {
                    Priorities.AddMissing(source.Name);
                }
                Priorities.AddMissing("Undefined");
                foreach(var sourceName in Priorities)
                {
                    if (plugin.PlayniteApi.Database.Sources.TryFind(s => s.Name == sourceName, out var source))
                    {
                        plugin.SettingsView.PriorityListBox.Items.Add(plugin.SettingsView.CreatePriorityEntry(source));
                    } else if (sourceName == "Undefined")
                    {
                        plugin.SettingsView.PriorityListBox.Items.Add(plugin.SettingsView.CreatePriorityEntry(null));
                    }
                }
            });
            plugin.SettingsView.PlatformComboBox.Items.Dispatcher.Invoke(() =>
            {
                List<CheckBox> checkBoxes = new List<CheckBox>();
                foreach (var platform in plugin.PlayniteApi.Database.Platforms.Concat(new List<Platform>{ null }))
                {
                    string platformName = platform != null ? platform.Name : "Undefined";
                    if (IncludePlatforms.Contains(platformName))
                    {
                        var cb = new CheckBox { Content = platformName, Tag = platform };
                        cb.IsChecked = true;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.PlatformComboBox.Items.Add(cb);
                    }
                }
                foreach (var platform in plugin.PlayniteApi.Database.Platforms.Concat(new List<Platform> { null }))
                {
                    string platformName = platform != null ? platform.Name : "Undefined";
                    if (!IncludePlatforms.Contains(platformName))
                    {
                        var cb = new CheckBox { Content = platformName, Tag = platform };
                        cb.IsChecked = false;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.PlatformComboBox.Items.Add(cb);
                    }
                }
                plugin.SettingsView.Platforms = checkBoxes;
            });
            plugin.SettingsView.SourceComboBox.Items.Dispatcher.Invoke(() =>
            {
                List<CheckBox> checkBoxes = new List<CheckBox>();
                foreach (var source in plugin.PlayniteApi.Database.Sources.Concat(new List<GameSource> { null }))
                {
                    string sourceName = source != null ? source.Name : "Undefined"; 
                    if (ExcludeSources.Contains(sourceName))
                    {
                        var cb = new CheckBox { Content = sourceName, Tag = source };
                        cb.IsChecked = true;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.SourceComboBox.Items.Add(cb);
                    }
                }
                foreach (var source in plugin.PlayniteApi.Database.Sources.Concat(new List<GameSource> { null }))
                {
                    string sourceName = source != null ? source.Name : "Undefined";
                    if (!ExcludeSources.Contains(sourceName))
                    {
                        var cb = new CheckBox { Content = sourceName, Tag = source };
                        cb.IsChecked = false;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.SourceComboBox.Items.Add(cb);
                    }
                }
                plugin.SettingsView.Sources = checkBoxes;
            });
            plugin.SettingsView.CategoriesComboBox.Items.Dispatcher.Invoke(() =>
            {
                List<CheckBox> checkBoxes = new List<CheckBox>();
                foreach (var category in plugin.PlayniteApi.Database.Categories)
                {
                    if (ExcludeCategories.Contains(category.Name))
                    {
                        var cb = new CheckBox { Content = category.Name, Tag = category };
                        cb.IsChecked = true;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.CategoriesComboBox.Items.Add(cb);
                    }
                }
                foreach (var category in plugin.PlayniteApi.Database.Categories)
                {
                    if (!ExcludeCategories.Contains(category.Name))
                    {
                        var cb = new CheckBox { Content = category.Name, Tag = category };
                        cb.IsChecked = false;
                        checkBoxes.Add(cb);
                        plugin.SettingsView.CategoriesComboBox.Items.Add(cb);
                    }
                }
                plugin.SettingsView.Categories = checkBoxes;
            });

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
                item.Content = game == null ? "Game not found: " + id.ToString() : $"{game.Name} ({(game.Source != null? game.Source.Name:"Undefined")})";
                item.ToolTip = item.Content;
                plugin.SettingsView.IgnoreListBox.Items.Add(item);
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
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SettingsView.AutoUpdateCheckBox.Dispatcher.Invoke(() =>
            {
                UpdateAutomatically = plugin.SettingsView.AutoUpdateCheckBox.IsChecked??false;
            });
            List<string> updatedPriorites = new List<string> { };
            plugin.SettingsView.PriorityListBox.Items.Dispatcher.Invoke(() =>
            {
                foreach (ListBoxItem item in plugin.SettingsView.PriorityListBox.Items)
                {
                    if (item.Tag is GameSource source)
                    {
                        updatedPriorites.AddMissing(source.Name);
                    } else
                    {
                        updatedPriorites.AddMissing("Undefined");
                    }
                }
            });
            Priorities = updatedPriorites;
            plugin.SettingsView.PlatformComboBox.Items.Dispatcher.Invoke(() =>
            {
                foreach (CheckBox cb in plugin.SettingsView.PlatformComboBox.Items)
                {
                    string name = cb.Content as string;
                    if (cb.IsChecked ?? false)
                    {
                        IncludePlatforms.AddMissing(name);
                    } else
                    {
                        IncludePlatforms.Remove(name);
                    }

                }
            });
            plugin.SettingsView.SourceComboBox.Items.Dispatcher.Invoke(() =>
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
            });
            plugin.SettingsView.CategoriesComboBox.Items.Dispatcher.Invoke(() =>
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
            });
            List<Guid> toIgnoreIds = new List<Guid>();
            IgnoredGames.Clear();
            plugin.SettingsView.IgnoreListBox.Dispatcher.Invoke(() =>
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