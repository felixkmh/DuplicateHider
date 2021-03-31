using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Playnite.SDK.Models;

// <ContentControl x:Name="DuplicateHider_SourceSelector" DockPanel.Dock="Right" MaxHeight="{Binding ElementName=PART_ImageIcon, Path=Height}"/>

namespace DuplicateHider
{
    /// <summary>
    /// Interaktionslogik für DHSourceSelector.xaml
    /// </summary>
    public partial class SourceSelector : Playnite.SDK.Controls.PluginUserControl
    {
        protected static ConcurrentDictionary<GameSource, BitmapImage> sourceIconCache 
            = new ConcurrentDictionary<GameSource, BitmapImage>();

        protected static System.Windows.Media.Effects.DropShadowEffect dropShadow = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.White,
            ShadowDepth = 0,
            BlurRadius = 10
        };


        protected static GameSource defaultGameSource = new GameSource("Undefined");

        protected readonly DuplicateHider duplicateHider = null;
        public List<string> UserIconFolderPaths { get; set; } = new List<string>();

        ~SourceSelector()
        {
            duplicateHider.GroupUpdated -= DuplicateHider_GroupUpdated;
        }

        public SourceSelector()
        {
            InitializeComponent();
        }

        public SourceSelector(DuplicateHider duplicateHider, Orientation orientation = Orientation.Horizontal) : this()
        {
            this.duplicateHider = duplicateHider;
            duplicateHider.GroupUpdated += DuplicateHider_GroupUpdated;
            UserIconFolderPaths.Add(System.IO.Path.Combine(
                duplicateHider.GetPluginUserDataPath(),
                "source_icons"
            ));
            IconStackPanel.Orientation = orientation;
        }


        private void DuplicateHider_GroupUpdated(object sender, IEnumerable<Guid> e)
        {
            if (GameContext is Game game)
            {
                if (e.Any(id => game.Id == id))
                {
                    UpdateGameSourceIcons(game);
                }
            }
        }

        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            UpdateGameSourceIcons(newContext);
        }

        public static string GetResourceIconUri(string sourceName)
        {
            var source = Uri.EscapeDataString(sourceName);
            var name = GetResourceNames()
                .Where(n => n.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(n => System.IO.Path.GetFileName(n).StartsWith(source, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(name))
            {
                return $"pack://application:,,,/DuplicateHider;component/{name}";
            }
            else
            {
                return null;
            }
        }

        // https://stackoverflow.com/a/2517799
        public static string[] GetResourceNames()
        {
            var asm = Assembly.GetAssembly(typeof(DuplicateHider));
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
                using (var reader = new System.Resources.ResourceReader(stream))
                {
                    return reader.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
                }
        }

        private void UpdateGameSourceIcons(Game context)
        {
            var games = GetGames(context);
            IconStackPanel.Children.Clear();
            if (games.Count() < 2) return;
            foreach (var game in games)
            {
                if (game == null) continue;
                var bt = new Button()
                {
                    BorderBrush = null,
                    Foreground = null,
                    Background = null,
                    Padding = new Thickness(0),
                    Margin = new Thickness(2, 0, 2, 0),
                    BorderThickness = new Thickness(0),
                    Tag = game,
                    ToolTip = duplicateHider.ExpandDisplayString(game, (duplicateHider.GetSettings(false) as DuplicateHiderSettings).DisplayString),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };

                bt.Click += Bt_Click;
                bt.MouseEnter += Icon_MouseEnter;
                bt.MouseLeave += Icon_MouseLeave;
                Image icon = new Image()
                {
                    Source = GetSourceIcon(game),
                    Stretch = Stretch.Uniform
                };
                icon.Opacity = game.IsInstalled ? 1.0 : 0.5;
                bt.Content = icon;
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
                IconStackPanel.Children.Add(bt);
            }
        }

        private void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Control img)
            {
                img.Effect = null;
            }
        }

        private void Icon_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Control img)
            {
                img.Effect = dropShadow;

            }
        }

        private void Bt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Control bt)
            {
                duplicateHider.PlayniteApi.StartGame((bt.Tag as Game).Id);
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
            if (duplicateHider is DuplicateHider dh)
            {
                if (game != null)
                {
                    return (new Game[] { game })
                            .Concat(dh.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => duplicateHider.GetGamePriority(g.Id))
                            .Take(MaxNumberOfIcons);
                }
            }

            return new Game[] { game };
        }

        protected ImageSource GetSourceIcon(Game game)
        {
            var source = game.Source ?? defaultGameSource;
            if (!sourceIconCache.ContainsKey(source))
            {
                if (sourceIcons is ResourceDictionary dict)
                {
                    if (dict.Contains(source.Name))
                    {
                        if (dict[source.Name] is BitmapImage icon)
                        {
                            sourceIconCache[source] = icon;
                        }
                    }
                } else {
                    BitmapImage image = null;
                    if (GetSourceIconPath(game) is string path)
                    {
                        image = new BitmapImage(new Uri(path));
                    }
                    sourceIconCache[source] = image;
                }
            }
            return sourceIconCache[source];
        }

        protected string GetSourceIconPath(Game game)
        {
            var name = game.Source != null ? game.Source.Name : "Undefined";
            var path = UserIconFolderPaths
               .SelectMany(s => System.IO.Directory.GetFiles(s))
               .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(name, StringComparison.OrdinalIgnoreCase))
               .FirstOrDefault(f =>
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
            if (path is null)
            {
                path = GetResourceIconUri(name);
            }
            if (path is null)
            {
                path = GetPluginIconPath(game);
            }
            return path is null ? GetDefaultIconPath() : path;
        }

        protected static string GetDefaultIconPath()
        {
            return "pack://application:,,,/DuplicateHider;component/icons/undefined.ico";
        }

        protected string GetPluginIconPath(Game game)
        {
            if (duplicateHider is DuplicateHider dh)
            {
                if (game.PluginId is Guid id)
                {
                    var plugin = dh.PlayniteApi.Addons.Plugins
                        .OfType<Playnite.SDK.Plugins.LibraryPlugin>()
                        .FirstOrDefault(p => p.Id == id);

                    if (plugin is Playnite.SDK.Plugins.LibraryPlugin lp)
                    {
                        var path = lp.LibraryIcon;
                        return path;
                    }
                }
            }
            return null;
        }

        [Description("Maximum number of icons displayed."), Category("Appearance")]
        public int MaxNumberOfIcons { get; set; } = 4;

        [Description("Height of each source icon."), Category("Appearance")]
        public Double IconHeight { get; set; } = Double.NaN;

        [Description("Orientation of the icon stack."), Category("Appearance")]
        public Orientation IconStackOrientation
        {
            get => IconStackPanel.Orientation;
            set => IconStackPanel.Orientation = value;
        }

        private ResourceDictionary sourceIcons;

        [Description("Source icon for each source."), Category("Data")]
        public ResourceDictionary SourceIconFolderPath
        {
            get => sourceIcons;
            set
            {
                sourceIcons = value;
                sourceIconCache.Clear();
                UpdateGameSourceIcons(GameContext);
            }
        }

        [Description("Height of each source icon."), Category("Data")]
        public List<ImageSource> Icons { get; set; }


    }

    
}
