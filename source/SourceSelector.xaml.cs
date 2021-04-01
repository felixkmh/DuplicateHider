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
using Playnite;
using Playnite.SDK.Models;

// <ContentControl x:Name="DuplicateHider_SourceSelector" DockPanel.Dock="Right" MaxHeight="{Binding ElementName=PART_ImageIcon, Path=Height}"/>
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider
{
    /// <summary>
    /// Interaktionslogik für DHSourceSelector.xaml
    /// </summary>
    public partial class SourceSelector : Playnite.SDK.Controls.PluginUserControl
    {
        internal static ConcurrentDictionary<GameSource, BitmapImage> SourceIconCache { get; set; }
            = new ConcurrentDictionary<GameSource, BitmapImage>();

        protected Game Context { get; set; } = null;

        protected static System.Windows.Media.Effects.DropShadowEffect dropShadow = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.White,
            ShadowDepth = 0,
            BlurRadius = 10
        };

        protected static System.Windows.Media.Effects.DropShadowEffect selectedEffect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Blue,
            ShadowDepth = 0,
            BlurRadius = 8
        };


        protected static GameSource defaultGameSource = new GameSource("Undefined");

        internal static DuplicateHider DuplicateHiderInstance { get; set; } = null;
        internal static List<string> UserIconFolderPaths { get; set; } = new List<string>();

        ~SourceSelector()
        {
            DuplicateHiderInstance.GroupUpdated -= DuplicateHider_GroupUpdated;
            IsVisibleChanged -= SourceSelector_IsVisibleChanged;
            DataContextChanged -= SourceSelector_DataContextChanged;
        }

        public SourceSelector()
        {
            InitializeComponent();
        }

        public SourceSelector(Orientation orientation = Orientation.Horizontal) : this()
        {
            IconStackPanel.Orientation = orientation;

            // duplicateHider.GroupUpdated += DuplicateHider_GroupUpdated;
            IsVisibleChanged += SourceSelector_IsVisibleChanged;
            DataContextChanged += SourceSelector_DataContextChanged;
            Context = GameContext;

            SetResourceReference(MaxNumberOfIconsProperty, "DuplicateHider_MaxNumberOfIcons");
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            
            base.OnVisualParentChanged(oldParent);
        }

        private void Parent_MouseLeave(object sender, MouseEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void Parent_MouseEnter(object sender, MouseEventArgs e)
        {
            Visibility = Visibility.Visible;
        }

        private void SourceSelector_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue as bool? == true)
            {
                DuplicateHiderInstance.GroupUpdated += DuplicateHider_GroupUpdated;
                UpdateGameSourceIcons(Context);
            } else
            {
                DuplicateHiderInstance.GroupUpdated -= DuplicateHider_GroupUpdated;
            }
        }

        private void SourceSelector_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                dynamic cont = e.NewValue;
                Guid id = cont.Id;
                var game = DuplicateHiderInstance.PlayniteApi.Database.Games.Get(id);
                Context = game;
                if (IsVisible)
                {
                    UpdateGameSourceIcons(Context);
                }
            }
            catch (Exception)
            {
            }
        }

        private void DuplicateHider_GroupUpdated(object sender, IEnumerable<Guid> e)
        {
            if (Context is Game game)
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

        internal static string GetResourceIconUri(string sourceName)
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
        internal static string[] GetResourceNames()
        {
            var asm = Assembly.GetAssembly(typeof(DuplicateHider));
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
                using (var reader = new System.Resources.ResourceReader(stream))
                {
                    return reader.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
                }
        }

        internal void UpdateGameSourceIcons(Game context)
        {
            var games = GetGames(context);
            IconStackPanel.Children.Clear();
            if (games.Count() < 2) return;
            if (DuplicateHiderInstance.GetSettings(false) is DuplicateHiderSettings settings) 
                if (!settings.EnableUiIntegration) 
                    return;
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
                    DataContext = game,
                    ToolTip = DuplicateHiderInstance.ExpandDisplayString(game, (DuplicateHiderInstance.GetSettings(false) as DuplicateHiderSettings).DisplayString),
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
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.Fant);
                icon.Opacity = game.IsInstalled ? 1.0 : 0.5;

                bt.MouseRightButtonUp += Bt_MouseRightButtonUp;
                bt.MouseDoubleClick += Bt_MouseDoubleClick;

                bt.Content = icon;
                IconStackPanel.Children.Add(bt);
            }
        }

        private void Bt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Control bt)
            {
                DuplicateHiderInstance.PlayniteApi.StartGame((bt.DataContext as Game).Id);
            }
        }

        private void Bt_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // TODO: bring up context menu for each individual game
            e.Handled = true;
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
                DuplicateHiderInstance.PlayniteApi.MainView.SelectGame(Context.Id);
                DuplicateHiderInstance.PlayniteApi.MainView.SelectGame((bt.DataContext as Game).Id);
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
            if (DuplicateHiderInstance is DuplicateHider dh)
            {
                if (game != null)
                {
                    return (new Game[] { game })
                            .Concat(dh.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => DuplicateHiderInstance.GetSourceRank(g))
                            .Take(MaxNumberOfIcons);
                }
            }

            return new Game[] { game };
        }

        protected ImageSource GetSourceIcon(Game game)
        {
            var source = game.Source ?? defaultGameSource;
            if (!SourceIconCache.ContainsKey(source))
            {
                if (sourceIcons is ResourceDictionary dict)
                {
                    if (dict.Contains(source.Name))
                    {
                        if (dict[source.Name] is BitmapImage icon)
                        {
                            SourceIconCache[source] = icon;
                        }
                    }
                } else {
                    BitmapImage image = null;
                    if (GetSourceIconPath(game) is string path)
                    {
                        image = new BitmapImage(new Uri(path));
                    }
                    SourceIconCache[source] = image;
                }
            }
            return SourceIconCache[source];
        }

        protected string GetSourceIconPath(Game game)
        {
            var name = game.Source != null ? game.Source.Name : "Undefined";
            bool enableThemeIcons = false;
            bool preferUserIcons = false;
            if (DuplicateHiderInstance.GetSettings(false) is DuplicateHiderSettings settings)
            {
                enableThemeIcons = settings.EnableThemeIcons;
                preferUserIcons = settings.PreferUserIcons;
            }

            List<string> paths = new List<string>();

            var userIconPath = GetUserIconPath(name);
            var themeIconPath = enableThemeIcons ? GetThemeIconPath(name) : null;
            var resourceIconPath = GetResourceIconUri(name);
            var pluginIconPath = GetPluginIconPath(game);
            if (preferUserIcons) paths.Add(userIconPath);
            if (enableThemeIcons) paths.Add(themeIconPath);
            paths.Add(resourceIconPath);
            if (!preferUserIcons) paths.Add(userIconPath);
            paths.Add(pluginIconPath);

            var path = paths.FirstOrDefault(p => !string.IsNullOrEmpty(p));

            return string.IsNullOrEmpty(path) ? GetDefaultIconPath() : path;
        }

        private string GetThemeIconPath(string sourceName)
        {
            if (DuplicateHiderInstance.PlayniteApi.Resources.GetResource($"DuplicateHider_{sourceName}_Icon") is BitmapImage img)
            {
                return img.UriSource.ToString();
            }
            return null;
        }

        private static string GetUserIconPath(string sourceName)
        {
            return UserIconFolderPaths
               .SelectMany(s => System.IO.Directory.GetFiles(s))
               .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(sourceName, StringComparison.OrdinalIgnoreCase))
               .FirstOrDefault(f =>
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
        }

        protected static string GetDefaultIconPath()
        {
            return "pack://application:,,,/DuplicateHider;component/icons/undefined.ico";
        }

        protected string GetPluginIconPath(Game game)
        {
            if (DuplicateHiderInstance is DuplicateHider dh)
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
        public Int32 MaxNumberOfIcons {
            get => (Int32)GetValue(MaxNumberOfIconsProperty);
            set => SetValue(MaxNumberOfIconsProperty, value);
        }
        public static readonly DependencyProperty MaxNumberOfIconsProperty = 
            DependencyProperty.Register(nameof(MaxNumberOfIcons), typeof(Int32), typeof(SourceSelector), new PropertyMetadata(4));

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
                SourceIconCache.Clear();
                UpdateGameSourceIcons(GameContext);
            }
        }

        [Description("Height of each source icon."), Category("Data")]
        public List<ImageSource> Icons { get; set; }


    }

    
}
