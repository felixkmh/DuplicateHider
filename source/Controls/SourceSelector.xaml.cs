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
namespace DuplicateHider.Controls
{
    /// <summary>
    /// Interaktionslogik für DHSourceSelector.xaml
    /// </summary>
    public partial class SourceSelector : Playnite.SDK.Controls.PluginUserControl
    {
        internal static readonly UnboundedCache<Button>[] ButtonCaches
            = new UnboundedCache<Button>[Constants.NUMBEROFSOURCESELECTORS];

        protected Game Context { get; set; } = null;

        private int selectorNumber = 0;

        protected static DropShadowEffect dropShadow = new DropShadowEffect
        {
            Color = Colors.White,
            ShadowDepth = 0,
            BlurRadius = 10
        };

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

            string key = "DuplicateHider_IconStackPanelStyle".Suffix(number);
            if (DuplicateHiderPlugin.DHP.PlayniteApi.Resources.
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
                DuplicateHiderPlugin.DHP.GroupUpdated += DuplicateHider_GroupUpdated;
                DataContextChanged += SourceSelector_DataContextChanged;
                try
                {
                    dynamic cont = DataContext;
                    Guid id = cont.Id;
                    var game = DuplicateHiderPlugin.DHP.PlayniteApi.Database.Games.Get(id);
                    Context = game;
                    UpdateGameSourceIcons(Context);
                }
                catch (Exception)
                {
                }
            } else
            {
                DataContextChanged -= SourceSelector_DataContextChanged;
                DuplicateHiderPlugin.DHP.GroupUpdated -= DuplicateHider_GroupUpdated;
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
                    var game = DuplicateHiderPlugin.DHP.PlayniteApi.Database.Games.Get(id);
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

        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            base.GameContextChanged(oldContext, newContext);
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

        internal void CreateGameSourceIcons()
        {
            if (!DuplicateHiderPlugin.DHP.settings.EnableUiIntegration) 
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
            if (DuplicateHiderPlugin.DHP.PlayniteApi.Resources.
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

            if (games.Count < 2 && !DuplicateHiderPlugin.DHP.settings.ShowSingleIcon)
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
                button.ToolTip = DuplicateHiderPlugin.DHP.ExpandDisplayString(game, DuplicateHiderPlugin.DHP.settings.DisplayString);
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
                DuplicateHiderPlugin.DHP.PlayniteApi.StartGame((bt.DataContext as Game).Id);
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
                DuplicateHiderPlugin.DHP.PlayniteApi.MainView.SelectGame((bt.DataContext as Game).Id);
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
            if (DuplicateHiderPlugin.DHP is DuplicateHiderPlugin dh)
            {
                if (game != null)
                {
                    return (new Game[] { game })
                            .Concat(dh.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => DuplicateHiderPlugin.DHP.GetGamePriority(g.Id))
                            .ThenByDescending(g=> g.Hidden)
                            .ThenBy(g => g.Id)
                            .Take(MaxNumberOfIcons);
                }
            }

            return new Game[] { };
        }

        protected ImageSource GetSourceIcon(Game game)
        {
            return DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(game);
        }

        [Description("Maximum number of icons displayed."), Category("Appearance")]
        public Int32 MaxNumberOfIcons
        {
            get => (Int32)GetValue(MaxNumberOfIconsProperty);
            set => SetValue(MaxNumberOfIconsProperty, value); 
        }

        public static DependencyProperty MaxNumberOfIconsProperty 
            = DependencyProperty.Register(nameof(MaxNumberOfIcons), typeof(Int32), typeof(SourceSelector), new PropertyMetadata(4));

        [Description("Orientation of the icon stack."), Category("Appearance")]
        public Orientation IconStackOrientation
        {
            get => IconStackPanel.Orientation;
            set => IconStackPanel.Orientation = value;
        }
    }
}
