using DuplicateHider.Models;
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
            = DependencyProperty.Register(nameof(CurrentGame), typeof(ListData), typeof(DHContentControl), new PropertyMetadata(null));

        public Boolean SwitchedGroup
        {
            get => (Boolean)GetValue(SwitchedGroupProperty);
            set => SetValue(SwitchedGroupProperty, value);
        }
        public static DependencyProperty SwitchedGroupProperty
            = DependencyProperty.Register(nameof(SwitchedGroup), typeof(Boolean), typeof(DHContentControl), new PropertyMetadata(true));

        public ICommand OpenMenuCommand {
            get => (ICommand)GetValue(OpenMenuCommandProperty);
            set => SetValue(OpenMenuCommandProperty, value);
        }
        public static DependencyProperty OpenMenuCommandProperty
            = DependencyProperty.Register(nameof(OpenMenuCommand), typeof(ICommand), typeof(DHContentControl), new PropertyMetadata(null));

        public Game GameContext { get; set; } = null;

        public DHContentControl()
        {
            InitializeComponent();
            DataContext = this;
            MouseDown += DHContentControl_MouseDown;
            IsVisibleChanged += DHContentControl_IsVisibleChanged;
            OpenMenuCommand = new SimpleCommand(() => {
                
            });
        }

        private void DHContentControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue as bool? == true)
            {
                DuplicateHiderPlugin.DHP.GroupUpdated += DHP_GroupUpdated;
                DuplicateHiderPlugin.DHP.GameSelected += DHP_GameSelected;
                if (CurrentGame?.Game?.Id != GameContext?.Id)
                {
                    CurrentGame = GameContext is Game ? new ListData(GameContext, true) : null;
                }
                UpdateContent(GameContext);
            } else
            {
                DuplicateHiderPlugin.DHP.GameSelected -= DHP_GameSelected;
                DuplicateHiderPlugin.DHP.GroupUpdated -= DHP_GroupUpdated;
            }
        }

        private void DHP_GameSelected(object sender, DuplicateHiderPlugin.GameSelectedArgs e)
        {
            foreach (var game in Games)
            {
                if (game is ListData data)
                {
                    data.IsCurrent = e.newId == data.Game.Id;
                }
            }
        }

        private void DHContentControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DHP_GroupUpdated(object sender, IEnumerable<Guid> e)
        {
            Dispatcher.Invoke(() =>
            {
                if (GameContext is Game game)
                {
                    if (e.TryFind(id => GameContext.Id == id, out var _))
                    {
                        UpdateContent(GameContext, true);
                    }
                }
                else
                {
                    Games.Clear();
                }
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Called Group Update"); 
#endif
            });
        }

        public DHContentControl(int n) : this()
        {
            
        }

        public void GameContextChanged(Game oldContext, Game newContext)
        {
            if (IsVisible)
            {
                if (CurrentGame?.Game?.Id != newContext?.Id)
                {
                    CurrentGame = newContext is Game ? new ListData(newContext, true) : null;
                }
                UpdateContent(newContext);
            }
        }

        private object ByAction { get; set; } = false;

        void setAction()
        {
            ByAction = true;
        }

        private void UpdateContent(Game newContext, bool forceUpdate = false)
        {
            if (newContext is Game)
            {
                ListData item = null;
                bool sameGroup = newContext is Game && Games.TryFind(g => g.Game.Id == newContext.Id, out item);
                SwitchedGroup = !sameGroup;// || forceUpdate;// && (Games.IndexOf(item) == 0 && ByAction);
                ByAction = false;
                if (sameGroup && !forceUpdate)
                {
                    foreach (var copy in Games)
                    {
                        if (copy.Game.Id == newContext.Id)
                        {
                            copy.IsCurrent = true;
                        }
                        else
                        {
                            copy.IsCurrent = false;
                        }

                    }
                }
                else
                {
                    var copys = GetGames(newContext);
                    MoreThanOneCopy = copys.Count() > 1;
                    Games.Clear();
                    foreach (var copy in copys)
                    {
                        var source = copy.Source ?? Constants.DEFAULT_SOURCE;
                        Games.Add(new ListData(copy, copy.Id == DuplicateHiderPlugin.DHP.CurrentlySelected));
                    }
                }
            } else
            {
                Games.Clear();
            }
        }

        private IEnumerable<Game> GetGames(Game game)
        {
                if (game != null)
                {
                    var copys = (new Game[] { game })
                            .Concat(DuplicateHiderPlugin.DHP.GetOtherCopies(game))
                            .Distinct()
                            .OrderBy(g => DuplicateHiderPlugin.DHP.GetGamePriority(g.Id))
                            .ThenBy(g => g.Hidden?1:-1)
                            .ThenBy(g => g.Id);

                    if (MaxNumberOfIconsCC > 0)
                        return copys.Take(MaxNumberOfIconsCC);
                    else
                        return copys;
                }

            return new Game[] { };
        }


        public Int32 MaxNumberOfIconsCC
        {
            get => (Int32)GetValue(MaxNumberOfIconsCCProperty);
            set => SetValue(MaxNumberOfIconsCCProperty, value);
        }

        public static DependencyProperty MaxNumberOfIconsCCProperty
            = DependencyProperty.Register(nameof(MaxNumberOfIconsCC), typeof(Int32), typeof(SourceSelector), new PropertyMetadata(4));
    }
}
