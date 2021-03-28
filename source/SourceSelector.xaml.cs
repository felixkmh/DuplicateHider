using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace DuplicateHider
{
    /// <summary>
    /// Interaktionslogik für DHSourceSelector.xaml
    /// </summary>
    public partial class SourceSelector : Playnite.SDK.Controls.PluginUserControl
    {
        protected static ConcurrentDictionary<GameSource, Image> sourceIconCache 
            = new ConcurrentDictionary<GameSource, Image>(); 

        protected readonly DuplicateHider duplicateHider = null;
        public List<string> UserIconFolderPaths { get; set; } = new List<string>();

        public SourceSelector()
        {
            InitializeComponent();
        }

        public SourceSelector(DuplicateHider duplicateHider) : this()
        {
            this.duplicateHider = duplicateHider;
            UserIconFolderPaths.Add(System.IO.Path.Combine(
                duplicateHider.GetPluginUserDataPath(),
                "source_icons"
            ));
            UpdateGameSourceIcons();
        }

        private void UpdateGameSourceIcons()
        {
            var games = GetGames();
            IconStackPanel.Children.Clear();
            foreach (var game in games)
            {
                var icon = GetSourceIcon(game);
                var bt = new Button();
                bt.Tag = game;
                bt.Click += Bt_Click;
                bt.MouseEnter += Bt_MouseEnter;
                bt.MouseLeave += Bt_MouseLeave;
                IconStackPanel.Children.Add(icon);
            }
        }

        private void Bt_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button bt)
            {
                bt.Effect = null;
            }
        }

        private void Bt_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button bt)
            {
                var dropShadow = new System.Windows.Media.Effects.DropShadowEffect();
                bt.Effect = new System.Windows.Media.Effects.DropShadowEffect();
            }
        }

        private void Bt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                duplicateHider.PlayniteApi.StartGame((bt.Tag as Game).Id);
            }
        }

        private IEnumerable<Game> GetGames()
        {
            if (duplicateHider is DuplicateHider dh)
            {
                if (GameContext is Game game)
                {
                    return (new Game[] { GameContext })
                            .Concat(dh.GetOtherCopies(game))
                            .Distinct()
                            .Take(MaxNumberOfIcons);
                }
            }

            return new Game[] { GameContext };
        }

        protected Image GetSourceIcon(Game game)
        {
            if (!sourceIconCache.ContainsKey(game.Source))
            {
                if (sourceIcons is ResourceDictionary dict)
                {
                    if (dict.Contains(game.Source.Name))
                    {
                        if (dict[game.Source.Name] is Image icon)
                        {
                            sourceIconCache[game.Source] = icon;
                        }
                    }
                } else {
                    var image = new Image();
                    if (GetSourceIconPath(game) is string path)
                    {
                        image.Source = new BitmapImage(new Uri(path));
                    } else
                    {
                        image.Source = new BitmapImage(new Uri("/DuplicateHider;component/Resources/default.png"));
                    }
                    sourceIconCache[game.Source] = image;
                }
            }
            return sourceIconCache[game.Source];
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
                 || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));
            if (path is null)
            {
                path = GetPluginIconPath(game);
            }
            return path is null ? GetDefaultIconPath() : path;
        }

        protected string GetDefaultIconPath()
        {
            return null;
        }

        protected string GetPluginIconPath(Game game)
        {
            if (duplicateHider is DuplicateHider dh)
            {
                var plugin = dh.PlayniteApi.Addons.Plugins
                    .OfType<Playnite.SDK.Plugins.LibraryPlugin>()
                    .FirstOrDefault(p => p.Id == game.PluginId);

                if (plugin is Playnite.SDK.Plugins.LibraryPlugin lp)
                {
                    var path = lp.LibraryIcon;
                    return path;
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
                UpdateGameSourceIcons();
            }
        }

        [Description("Height of each source icon."), Category("Data")]
        public List<ImageSource> Icons { get; set; }
    }
}
