using DuplicateHider.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider.Models
{
    public class CustomGroupViewModel
    {
        public CustomGroup Group { get; set; } = null;
        public bool Synchronize { get; set; } = false;
        private string name = null;
        public string Name
        {
            get => name;
            set { name = value; if (Synchronize) Group.Name = name; }
        }
        private bool scoreByOrder = false;
        public bool ScoreByOrder
        {
            get => scoreByOrder;
            set { scoreByOrder = value; if (Synchronize) Group.ScoreByOrder = scoreByOrder; }
        }
        public ObservableCollection<Game> games = null;
        public ObservableCollection<Game> Games
        {
            get
            {
                if (games == null)
                {
                    var list = Group.Games.Select(game => DuplicateHiderPlugin.API.Database.Games.Get(game))
                                   .Where(game => game != null).ToList();
                    games = new ObservableCollection<Game>(list);
                    games.CollectionChanged += Games_CollectionChanged;
                }
                return games;
            }
        }

        public void UpdateGroup()
        {
            if (Games != null)
            {
                Group.Games.Clear();
                foreach(var game in Games)
                {
                    Group.Games.Add(game.Id);
                }

                Group.ScoreByOrder = scoreByOrder;
                Group.Name = name;
            }
        }

        private void Games_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Synchronize)
            {
                Group.Games.Clear();
                foreach(var game in Games)
                {
                    Group.Games.Add(game.Id);
                }
            }
        }

        public CustomGroupViewModel(CustomGroup group)
        {
            Group = group;
            name = group.Name;
            scoreByOrder = group.ScoreByOrder;
        } 
    }

    public class CustomGroupsViewModel
    {
        public IList<CustomGroup> GroupsSource = null;
        public ObservableCollection<CustomGroupViewModel> Groups { get; set; } = new ObservableCollection<CustomGroupViewModel>();
        public bool Synchronize { get; set; } = false;

        private void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (Synchronize)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    foreach (CustomGroupViewModel group in e.OldItems)
                    {
                        if (group != null)
                            GroupsSource.Remove(group.Group);
                    }
                }
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (CustomGroupViewModel group in e.NewItems)
                    {
                        if (group != null)
                            GroupsSource.Add(group.Group);
                    }
                }
            }
        }

        public CustomGroupsViewModel(IList<CustomGroup> groups)
        {
            GroupsSource = groups;
            foreach(var group in GroupsSource)
            {
                Groups.Add(new CustomGroupViewModel(group));
            }
            Groups.CollectionChanged += Groups_CollectionChanged;
        }
    }
}
