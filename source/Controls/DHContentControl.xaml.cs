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
            = DependencyProperty.Register(nameof(CurrentGameProperty), typeof(ListData), typeof(DHContentControl), new PropertyMetadata(null));

        public Boolean SwitchedGroup
        {
            get => (Boolean)GetValue(SwitchedGroupProperty);
            set => SetValue(SwitchedGroupProperty, value);
        }
        public static DependencyProperty SwitchedGroupProperty
            = DependencyProperty.Register(nameof(SwitchedGroupProperty), typeof(Boolean), typeof(DHContentControl), new PropertyMetadata(true));

        public Game GameContext { get; set; } = null;

        public DHContentControl()
        {
            InitializeComponent();
            DataContext = this;
            DuplicateHiderPlugin.DHP.GroupUpdated += DHP_GroupUpdated;
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
                System.Diagnostics.Debug.WriteLine("Called Group Update");
            });
        }

        public DHContentControl(int n) : this()
        {
            
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        public void GameContextChanged(Game oldContext, Game newContext)
        {
            if (newContext?.Id != CurrentGame?.Game?.Id)
            {
                CurrentGame = new ListData(DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(newContext), newContext, true);
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
                SwitchedGroup = !sameGroup;// && (Games.IndexOf(item) == 0 && ByAction);
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
                        Games.Add(new ListData(DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(copy), copy, copy.Id == newContext.Id));
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
