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

        public ObservableCollection<ListData> Games { get; set; } = new ObservableCollection<ListData>();

        public Boolean MoreThanOneCopy {
            get => (Boolean)GetValue(MoreThanOneGameProperty);
            set => SetValue(MoreThanOneGameProperty, value);
        }
        public static DependencyProperty MoreThanOneGameProperty 
            = DependencyProperty.Register(nameof(MoreThanOneCopy), typeof(Boolean), typeof(DHContentControl), new PropertyMetadata(false));

        public ListData CurrentGame
        {
            get => (ListData)GetValue(CurrentGameProperty);
            set => SetValue(CurrentGameProperty, value);
        }
        public static DependencyProperty CurrentGameProperty
            = DependencyProperty.Register(nameof(CurrentGameProperty), typeof(ListData), typeof(DHContentControl), new PropertyMetadata(null));

        internal Game GameContext = null;

        public DHContentControl()
        {
            InitializeComponent();
            DataContext = this;
            DuplicateHiderPlugin.DHP.GroupUpdated += DHP_GroupUpdated;
        }

        private void DHP_GroupUpdated(object sender, IEnumerable<Guid> e)
        {
            if (GameContext is Game game)
            {
                if (e.TryFind(id => GameContext.Id == id, out var _))
                {
                    UpdateContent(GameContext, true);
                }
            } else
            {
                Games.Clear();
            }
        }

        public DHContentControl(int n) : this()
        {
            
        }

        public class ListData : DependencyObject
        {
            public ImageSource Icon
            {
                get => (ImageSource)GetValue(IconProperty);
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
            public ICommand InstallCommand { get; set; }
            public ICommand UninstallCommand { get; set; }

            public ListData(ImageSource image, Game game, bool current = false)
            {
                var dp = new DockPanel();

                Icon = image;
                Game = game;
                IsCurrent = current;
                SourceName = game.Source?.Name ?? Constants.UNDEFINED_SOURCE;
                LaunchCommand = new SimpleCommand(() => DuplicateHiderPlugin.API.StartGame(Game.Id));
                SelectCommand = new SimpleCommand(() => DuplicateHiderPlugin.API.MainView.SelectGame(Game.Id));
                InstallCommand = new SimpleCommand(() => DuplicateHiderPlugin.API.InstallGame(Game.Id));
                UninstallCommand = new SimpleCommand(() => DuplicateHiderPlugin.API.InstallGame(Game.Id));
            }

            public static readonly DependencyProperty IsCurrentProperty 
                = DependencyProperty.Register(nameof(IsCurrent), typeof(Boolean), typeof(ListData), new PropertyMetadata(false));
            public static readonly DependencyProperty GameProperty
                = DependencyProperty.Register(nameof(Game), typeof(Game), typeof(ListData), new PropertyMetadata(null));
            public static readonly DependencyProperty IconProperty
                = DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(ListData), new PropertyMetadata(null));
            public static readonly DependencyProperty SourceNameProperty
                = DependencyProperty.Register(nameof(SourceName), typeof(String), typeof(ListData), new PropertyMetadata("Playnite"));
        }



        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public void GameContextChanged(Game oldContext, Game newContext)
        {
            GameContext = newContext;
            UpdateContent(newContext);
            if (GameContext is Game game)
            {
                CurrentGame = new ListData(DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(game), game, true);
            }
        }

        private void UpdateContent(Game newContext, bool forceUpdate = false)
        {
            if (newContext is Game && Games.TryFind(g => g.Game.Id == newContext.Id, out var _) && !forceUpdate)
            {
                foreach (var item in Games)
                {
                    if (item.Game.Id == newContext.Id)
                    {
                        item.IsCurrent = true;
                    }
                    else
                    {
                        item.IsCurrent = false;
                    }

                }
            }
            else
            {
                var copys = GetGames(newContext);
                MoreThanOneCopy = copys.Count() > 1;
                Games.Clear();
                foreach (var item in copys)
                {
                    var source = item.Source ?? Constants.DEFAULT_SOURCE;
                    Games.Add(new ListData(DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(item), item, item.Id == newContext.Id));
                }
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
                if (game != null)
                {
                    return (new Game[] { game })
                            .Concat(DuplicateHiderPlugin.DHP.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => DuplicateHiderPlugin.DHP.GetGamePriority(g.Id))
                            .ThenBy(g => g.Hidden?1:-1)
                            .ThenBy(g => g.Id);
                }

            return new Game[] { };
        }

    }
}
