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
        internal static readonly Dictionary<GameSource, BitmapImage> SourceIconCache
            = new Dictionary<GameSource, BitmapImage>();

        internal static readonly UnboundedCache<Button>[] ButtonCaches
            = new UnboundedCache<Button>[Constants.NUMBEROFSOURCESELECTORS];

        internal static List<string> ElementNames = new List<string>();

        protected Game Context { get; set; } = null;

        private int selectorNumber = 0;

        protected static DropShadowEffect dropShadow = new DropShadowEffect
        {
            Color = Colors.White,
            ShadowDepth = 0,
            BlurRadius = 10
        };


        protected static readonly GameSource defaultGameSource = new GameSource("Undefined");

        internal static DuplicateHider DuplicateHiderInstance { get; set; } = null;
        internal static List<string> UserIconFolderPaths { get; set; } = new List<string>();

        public SourceSelector()
        {
            InitializeComponent();
            Unloaded += SourceSelector_Unloaded; 
            IsVisibleChanged += SourceSelector_IsVisibleChanged;
            IconStackPanel.Unloaded += IconStackPanel_Unloaded;
            IconStackPanel.IsEnabledChanged += IconStackPanel_IsEnabledChanged;
        }

        public SourceSelector(int number, Orientation orientation = Orientation.Horizontal) : this()
        {
            selectorNumber = number;

            if (ButtonCaches[selectorNumber] == null)
            {
                ButtonCaches[selectorNumber] = new UnboundedCache<Button>(CreateSourceIcon);
            }

            var suffix = selectorNumber == 0 ? "" : selectorNumber.ToString();
            string key = $"DuplicateHider_IconStackPanelStyle{suffix}";
            if (DuplicateHiderInstance.PlayniteApi.Resources.
                GetResource(key) is Style style && style.TargetType == typeof(StackPanel))
            {
                IconStackPanel.SetResourceReference(StackPanel.StyleProperty, key);
            } else
            {
                IconStackPanel.Orientation = orientation;
            }

            Context = GameContext;
        }

        private void SourceSelector_Unloaded(object sender, RoutedEventArgs e)
        {
            if (e.Source is SourceSelector selector)
                ButtonCaches[selectorNumber].Consume(selector.IconStackPanel.Children);
        }

        private void IconStackPanel_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue as bool? == false && sender is StackPanel panel)
            {
                ButtonCaches[selectorNumber].Consume(panel.Children);
            }
        }

        private void IconStackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is StackPanel panel)
            {
                ButtonCaches[selectorNumber].Consume(panel.Children);
            }
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
                    UpdateGameSourceIcons(Context);
                }
                catch (Exception)
                {
                }
            } else
            {
                DataContextChanged -= SourceSelector_DataContextChanged;
                DuplicateHiderInstance.GroupUpdated -= DuplicateHider_GroupUpdated;
                ButtonCaches[selectorNumber].Consume(IconStackPanel.Children);
            }
        }

        private void SourceSelector_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ButtonCaches[selectorNumber].Consume(IconStackPanel.Children);
            try
            {
                if (e.NewValue != null)
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
            if (!DuplicateHiderInstance.settings.EnableUiIntegration) 
                return;
            for (int i = 0; i < MaxNumberOfIcons; ++i)
            {
                    IconStackPanel.Children.Add(ButtonCaches[selectorNumber].Get());
            }
        }


        private Button CreateSourceIcon()
        {
            var bt = new Button();
            var suffix = selectorNumber == 0 ? "" : selectorNumber.ToString();
            string key = $"DuplicateHider_IconButtonStyle{suffix}";
            if (DuplicateHiderInstance.PlayniteApi.Resources.
                GetResource(key) is Style style && style.TargetType == typeof(Button))
            {
                bt.SetResourceReference(Button.StyleProperty, key);
            } else
            {
                bt.BorderBrush = null;
                bt.Foreground = null;
                bt.Background = null;
                bt.Padding = new Thickness(0);
                bt.Margin = new Thickness(2, 0, 2, 0);
                bt.BorderThickness = new Thickness(0);
                bt.HorizontalAlignment = HorizontalAlignment.Stretch;
                bt.VerticalAlignment = VerticalAlignment.Center;
                bt.MouseEnter += Icon_MouseEnter;
                bt.MouseLeave += Icon_MouseLeave;
            }
           

            bt.Click += Bt_Click;
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

            if (games.Count < 2 && !DuplicateHiderInstance.settings.ShowSingleIcon)
            {
                ButtonCaches[selectorNumber].Consume(IconStackPanel.Children);
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
                    button = ButtonCaches[selectorNumber].Get();
                    IconStackPanel.Children.Add(button);
                }
                Game game = games[i];
                button.Visibility = Visibility.Visible;
                button.DataContext = game;
                button.ToolTip = DuplicateHiderInstance.ExpandDisplayString(game, DuplicateHiderInstance.settings.DisplayString);
                if (button.Content is Image icon)
                {
                    icon.Source = GetSourceIcon(game);
                    icon.Opacity = game.IsInstalled ? 1.0 : 0.5;
                }
            }
            for (int i = IconStackPanel.Children.Count - 1; i > games.Count - 1; --i)
            {
                Button button = IconStackPanel.Children[i] as Button;
                ButtonCaches[selectorNumber].Push(button);
                IconStackPanel.Children.RemoveAt(i);
            }

        }

        private static void Bt_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Control bt)
            {
                DuplicateHiderInstance.PlayniteApi.StartGame((bt.DataContext as Game).Id);
            }
        }

        private static void Bt_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // TODO: bring up context menu for each individual game
            e.Handled = true;
        }

        private static void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button bt)
            {
                bt.Effect = null;
            }
        }

        private static void Icon_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button bt)
            {
                bt.Effect = dropShadow;

            }
        }

        private static void Bt_Click(object sender, RoutedEventArgs e)
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
                        image.Freeze();
                    }
                    SourceIconCache[source] = image;
                }
            }
            return SourceIconCache[source];
        }

        protected string GetSourceIconPath(Game game)
        {
            var name = game.Source != null ? game.Source.Name : "Undefined";
            bool enableThemeIcons = DuplicateHiderInstance.settings.EnableThemeIcons;
            bool preferUserIcons = DuplicateHiderInstance.settings.PreferUserIcons;

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

        private static string GetThemeIconPath(string sourceName)
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
        public Int32 MaxNumberOfIcons
        {
            get => (Int32)GetValue(MaxNumberOfIconsProperty);
            set => SetValue(MaxNumberOfIconsProperty, value); 
        }
        public static DependencyProperty MaxNumberOfIconsProperty 
            = DependencyProperty.Register(nameof(MaxNumberOfIcons), typeof(Int32), typeof(SourceSelector), new PropertyMetadata(4));
        
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
