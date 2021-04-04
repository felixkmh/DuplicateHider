using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider.Controls
{
    /// <summary>
    /// Interaktionslogik für DHContentControl.xaml
    /// </summary>
    public partial class DHContentControl : ContentControl
    {
        internal static ConcurrentDictionary<GameSource, BitmapImage> SourceIconCache
            = new ConcurrentDictionary<GameSource, BitmapImage>();

        internal static DuplicateHiderPlugin DuplicateHiderInstance { get; set; } = null;

        public DHContentControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public DHContentControl(int n) : this()
        {
            
        }

        public class ListData : DependencyObject
        {
            public BitmapImage Icon
            {
                get => (BitmapImage)GetValue(IconProperty);
                set => SetValue(IconProperty, value);
            }
            public Game Game
            {
                get => (Game)GetValue(GameProperty);
                set => SetValue(GameProperty, value);
            }
            public String SourceName
            {
                get => (String)GetValue(SourceNameProperty);
                set => SetValue(SourceNameProperty, value);
            }
            public Boolean IsCurrent { 
                get => (Boolean)GetValue(IsCurrentProperty);
                set => SetValue(IsCurrentProperty, value);
            }
            public ICommand LaunchCommand { get; set; }
            public ICommand SelectCommand { get; set; }

            public ListData(BitmapImage image, Game game, bool current = false)
            {
                var dp = new DockPanel();

                Icon = image;
                Game = game;
                IsCurrent = current;
                SourceName = game.Source?.Name ?? Constants.UNDEFINED_SOURCE;
                LaunchCommand = new SimpleCommand(() => DuplicateHiderInstance.PlayniteApi.StartGame(Game.Id));
                SelectCommand = new SimpleCommand(() => DuplicateHiderInstance.PlayniteApi.MainView.SelectGame(Game.Id));
            }

            public static readonly DependencyProperty IsCurrentProperty 
                = DependencyProperty.Register(nameof(IsCurrent), typeof(Boolean), typeof(ListData), new PropertyMetadata(false));
            public static readonly DependencyProperty GameProperty
                = DependencyProperty.Register(nameof(Game), typeof(Game), typeof(ListData), new PropertyMetadata(null));
            public static readonly DependencyProperty IconProperty
                = DependencyProperty.Register(nameof(Icon), typeof(BitmapImage), typeof(ListData), new PropertyMetadata(null));
            public static readonly DependencyProperty SourceNameProperty
                = DependencyProperty.Register(nameof(SourceName), typeof(String), typeof(ListData), new PropertyMetadata("Playnite"));
        }

        public ObservableCollection<ListData> Games { get; set; } = new ObservableCollection<ListData>();


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public void GameContextChanged(Game oldContext, Game newContext)
        {
            Games.Clear();
            var copys = GetGames(newContext);
            foreach (var item in copys)
            {
                var source = item.Source ?? Constants.DEFAULT_SOURCE;
                Games.Add(new ListData(SourceIconCache.FirstOrDefault((e => e.Key == source)).Value, item, item.Id == newContext.Id));
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
            if (DuplicateHiderInstance is DuplicateHiderPlugin dh)
            {
                if (game != null)
                {
                    return (new Game[] { game })
                            .Concat(dh.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => DuplicateHiderInstance.GetGamePriority(g.Id))
                            .ThenBy(g => g.Hidden?1:-1)
                            .ThenBy(g => g.Id);
                }
            }

            return new Game[] { };
        }

    }
}
