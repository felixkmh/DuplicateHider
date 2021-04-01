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
using System.Windows.Media.Effects;

// <ContentControl x:Name="DuplicateHider_SourceSelector" DockPanel.Dock="Right" MaxHeight="{Binding ElementName=PART_ImageIcon, Path=Height}"/>
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider
{
    /// <summary>
    /// Interaktionslogik für DHSourceSelector.xaml
    /// </summary>
    public partial class SourceSelector : Playnite.SDK.Controls.PluginUserControl
    {
        internal static Dictionary<GameSource, BitmapImage> SourceIconCache { get; set; }
            = new Dictionary<GameSource, BitmapImage>();

        internal static Stack<Button> ButtonCache = new Stack<Button>();

        protected Game Context { get; set; } = null;

        protected static DropShadowEffect dropShadow = new DropShadowEffect
        {
            Color = Colors.White,
            ShadowDepth = 0,
            BlurRadius = 10
        };


        protected static GameSource defaultGameSource = new GameSource("Undefined");

        internal static DuplicateHider DuplicateHiderInstance { get; set; } = null;
        internal static List<string> UserIconFolderPaths { get; set; } = new List<string>();

        ~SourceSelector()
        {
            IsVisibleChanged -= SourceSelector_IsVisibleChanged;
        }

        public SourceSelector()
        {
            InitializeComponent();
        }

        public override void EndInit()
        {
            CreateGameSourceIcons();
            base.EndInit();
        }

        public SourceSelector(Orientation orientation = Orientation.Horizontal) : this()
        {
            IconStackPanel.Orientation = orientation;

            // duplicateHider.GroupUpdated += DuplicateHider_GroupUpdated;
            IsVisibleChanged += SourceSelector_IsVisibleChanged;
            
            Context = GameContext;
        }

        private void SourceSelector_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue as bool? == true)
            {
                DuplicateHiderInstance.GroupUpdated += DuplicateHider_GroupUpdated;
                DataContextChanged += SourceSelector_DataContextChanged;
                try
                {
                    dynamic cont = DataContext;
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
            } else
            {
                DataContextChanged -= SourceSelector_DataContextChanged;
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
            /*if (newContext != null)
            {
                Context = newContext;
                UpdateGameSourceIcons(newContext);
            }*/
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

        internal void CreateGameSourceIcons()
        {
            if (DuplicateHiderInstance.GetSettings(false) is DuplicateHiderSettings settings) 
                if (!settings.EnableUiIntegration) 
                    return;
            for (int i = 0; i < MaxNumberOfIcons; ++i)
            {
                if (ButtonCache.Count > 0)
                {
                    IconStackPanel.Children.Add(ButtonCache.Pop());
                } else
                {
                    Button bt = CreateSourceIcon();
                    IconStackPanel.Children.Add(bt);
                }
            }
        }

        private Button CreateSourceIcon()
        {
            var bt = new Button()
            {
                BorderBrush = null,
                Foreground = null,
                Background = null,
                Padding = new Thickness(0),
                Margin = new Thickness(2, 0, 2, 0),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };


            bt.Click += Bt_Click;
            bt.MouseEnter += Icon_MouseEnter;
            bt.MouseLeave += Icon_MouseLeave;
            Image icon = new Image()
            {
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

            bt.MouseRightButtonUp += Bt_MouseRightButtonUp;
            bt.MouseDoubleClick += Bt_MouseDoubleClick;

            bt.Content = icon;
            bt.Visibility = Visibility.Collapsed;
            return bt;
        }

        internal void UpdateGameSourceIcons(Game context)
        {
            var games = GetGames(context).ToList();

            if (games.Count < 2)
            {
                foreach (Button button in IconStackPanel.Children)
                {
                    ButtonCache.Push(button);
                }
                IconStackPanel.Children.Clear();
                return;
            }

            for(int i = 0; i < games.Count; ++i)
            {
                Button button = null;
                if (i < IconStackPanel.Children.Count)
                {
                    button = IconStackPanel.Children[i] as Button;
                } else
                {
                    if (ButtonCache.Count > 0)
                    {
                        button = ButtonCache.Pop();
                    } else
                    {
                        button = CreateSourceIcon();
                    }
                    IconStackPanel.Children.Add(button);
                }
                Game game = games[i];
                button.Visibility = Visibility.Visible;
                button.DataContext = game;
                button.ToolTip = DuplicateHiderInstance.ExpandDisplayString(game, (DuplicateHiderInstance.GetSettings(false) as DuplicateHiderSettings).DisplayString);
                if (button.Content is Image icon)
                {
                    icon.Source = GetSourceIcon(game);
                    icon.Opacity = game.IsInstalled ? 1.0 : 0.5;
                }
            }
            for (int i = IconStackPanel.Children.Count - 1; i > games.Count - 1; --i)
            {
                Button button = IconStackPanel.Children[i] as Button;
                ButtonCache.Push(button);
                IconStackPanel.Children.RemoveAt(i);
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
            if (sender is Button bt)
            {
                bt.Effect = null;
            }
        }

        private void Icon_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button bt)
            {
                bt.Effect = dropShadow;

            }
        }

        private void Bt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
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

            return new Game[] { };
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
        public static Int32 MaxNumberOfIcons { get; set; } = 4;
        
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
