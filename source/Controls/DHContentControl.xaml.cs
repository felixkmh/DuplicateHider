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
            public BitmapImage Icon { get; set; }
            public Game Game { get; set; }
            public Boolean IsCurrent { 
                get => (Boolean)GetValue(IsCurrentProperty); 
                set {
                    SetValue(IsCurrentProperty, value);
                }
            }

            public ListData(BitmapImage image, Game game, bool current = false)
            {
                Icon = image;
                Game = game;
                IsCurrent = current;
            }

            public static readonly DependencyProperty IsCurrentProperty 
                = DependencyProperty.Register(nameof(IsCurrent), typeof(Boolean), typeof(ListData), new PropertyMetadata(false));
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
                Games.Add(new ListData(SourceIconCache.FirstOrDefault((e => e.Key == item.Source)).Value, item, item.Id == newContext.Id));
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
                            .OrderBy(g => DuplicateHiderInstance.GetSourceRank(g));
                }
            }

            return new Game[] { };
        }

    }
}
