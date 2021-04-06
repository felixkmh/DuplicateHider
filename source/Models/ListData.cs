using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DuplicateHider")]
namespace DuplicateHider.Models
{
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
        public Boolean IsCurrent
        {
            get => (Boolean)GetValue(IsCurrentProperty);
            set => SetValue(IsCurrentProperty, value);
        }
        public ICommand LaunchCommand { get; set; }
        public ICommand SelectCommand { get; set; }
        public ICommand InstallCommand { get; set; }
        public ICommand UninstallCommand { get; set; }

        public ListData()
        {

        }

        public ListData(Game game, bool current) 
            : this(DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(game), game, current)
        {

        }

        public ListData(ImageSource image, Game game, bool current = false)
        {
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
}
