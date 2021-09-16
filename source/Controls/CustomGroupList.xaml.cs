using DuplicateHider.Data;
using DuplicateHider.Models;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DuplicateHider.Views
{
    /// <summary>
    /// Interaktionslogik für CustomGroupList.xaml
    /// </summary>
    public partial class CustomGroupList : UserControl
    {
        public CustomGroupList()
        {
            InitializeComponent();
        }

        public void AddGroup(CustomGroup group)
        {
            if (DataContext is CustomGroupsViewModel model)
            {
                model.Groups.Add(new CustomGroupViewModel(group));
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void DeleteGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                if (lb.ItemsSource is ObservableCollection<Game> cl && lb.SelectedItems is IList games)
                {
                    e.CanExecute = games.Count > 0;
                }
            }
            e.Handled = true;
        }

        private void DeleteGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if(sender is ListBox lb)
            {
                if (lb.ItemsSource is ObservableCollection<Game> cl && lb.SelectedItems is IList games)
                {
                    for (int i = games.Count - 1; i >= 0; --i)
                    {
                        if (games[i] is Game game)
                        {
                            cl.Remove(game);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        private void DeleteGroup_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void DeleteGroup_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button bt && bt.DataContext is CustomGroupViewModel group)
            {
                if (GroupsControl.ItemsSource is ObservableCollection<CustomGroupViewModel> cl)
                {
                    cl.Remove(group);
                }
            }
        }

        private void CutGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                if (lb.ItemsSource is ObservableCollection<Game> cl && lb.SelectedItems is IList games)
                {
                    e.CanExecute = games.Count > 0;
                }
            }
            e.Handled = true;
        }

        private readonly List<Game> clipboard = new List<Game>();

        private void CutGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            clipboard.Clear();
            if (sender is ListBox lb)
            {
                if (lb.ItemsSource is ObservableCollection<Game> cl && lb.SelectedItems is IList games)
                {
                    for (int i = games.Count - 1; i >= 0; --i)
                    {
                        if (games[i] is Game game)
                        {
                            clipboard.Add(game);
                            cl.Remove(game);
                        }
                    }
                }
            }
            e.Handled = true;
        }

        private void PasteGame_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = clipboard.Count > 0;
            e.Handled = true;
        }

        private void PasteGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is ListBox lb)
            {
                if (lb.ItemsSource is ObservableCollection<Game> cl)
                {
                    int j = Math.Max(lb.SelectedIndex, 0);
                    lb.SelectedItems.Clear();
                    for (int i = clipboard.Count - 1; i >= 0; --i)
                    {
                        cl.Insert(j++, clipboard[i]);
                        lb.SelectedItems.Add(clipboard[i]);
                    }
                }
            }
            clipboard.Clear();
            e.Handled = true;
        }

        private void GameList_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

        }

        private void GameList_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void Expander_LostFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is Expander exp && exp.Content is ListBox lb)
            {
                lb.SelectedIndex = -1;
            }
        }

        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {

        }

        Point startPosition;
        List<Game> draggedGames;
        object selectedItem = null;

        private void GameList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            startPosition = e.GetPosition(null);
            if (e.Source is ListBox lb && !contextMenuOpen &&
                e.OriginalSource is FrameworkElement element &&
                element.DataContext is Game hoveredGame &&
                Keyboard.Modifiers == ModifierKeys.None)
            {
                var selected = lb.SelectedItems.Cast<Game>().ToList();
                if (selected.Contains(hoveredGame))
                {
                    draggedGames = selected;
                }
                else
                {
                    draggedGames = new List<Game> { hoveredGame };
                }
                lb.SelectedItems.Clear();
                foreach (var game in draggedGames)
                {
                    lb.SelectedItems.Add(game);
                }
            }
            e.Handled = true;
        }

        bool dragging = false;

        private void GameList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            startPosition = e.GetPosition(null);
            var newSelection = new List<Game>();
            if (!dragging && !contextMenuOpen &&
                e.Source is ListBox lb &&
                e.OriginalSource is FrameworkElement element &&
                element.DataContext is Game hoveredGame)
            {
                var selected = lb.SelectedItems.Cast<Game>().ToList();
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (selected.Contains(hoveredGame))
                    {
                        newSelection = selected;
                        newSelection.Remove(hoveredGame);
                    }
                    else
                    {
                        selected.Add(hoveredGame);
                        newSelection = selected;
                    }
                }
                else if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    if (selected.Count > 0)
                    {
                        var last = selected.Last();
                        var lastContainer = lb.ItemContainerGenerator.ContainerFromItem(last);
                        var lastIndex = lb.ItemContainerGenerator.IndexFromContainer(lastContainer);
                        var hoveredContainer = lb.ItemContainerGenerator.ContainerFromItem(hoveredGame);
                        var hoveredIndex = lb.ItemContainerGenerator.IndexFromContainer(hoveredContainer);
                        var start = Math.Min(lastIndex, hoveredIndex);
                        var end = Math.Max(lastIndex, hoveredIndex);
                        newSelection = lb.ItemsSource.Cast<Game>().ToList().GetRange(start, end - start + 1);
                    }
                    else
                    {
                        newSelection = new List<Game> { hoveredGame };
                    }
                }
                else
                {
                    newSelection = new List<Game> { hoveredGame };
                }
                
                lb.SelectedItems.Clear();
                foreach (var game in newSelection)
                {
                    lb.SelectedItems.Add(game);
                }
                if (newSelection.Count > 0)
                {
                    var last = lb.ItemContainerGenerator.ContainerFromItem(newSelection.Last()) as FrameworkElement;
                    last?.Focus();
                }
            }
            dragging = false;
        }

        private void GameList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is ListBox lb && (draggedGames?.Count > 0))
            {
                if (e.LeftButton == MouseButtonState.Pressed && !contextMenuOpen)
                {
                    selectedItem = lb.SelectedItem;
                    var currentPos = e.GetPosition(null);
                    var delta = currentPos - startPosition;

                    lb.SelectedItems.Clear();
                    foreach (var game in draggedGames)
                    {
                        lb.SelectedItems.Add(game);
                    }

                    e.Handled = true;

                    if (Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(delta.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        dragging = true;
                        DataObject data = new DataObject();
                        data.SetData("Games", draggedGames);
                        data.SetData("SourceGroup", e.Source);
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void Expander_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData("Games") is IList<Game> games &&
                e.Data.GetData("SourceGroup") is ListBox source)
            {
                if (source.ItemsSource is ObservableCollection<Game> sourceItems)
                {
                    foreach(var game in games)
                    {
                        sourceItems.Remove(game);
                    }
                }
                ObservableCollection<Game> targetItems = null;
                IList selected = null;
                int insertionStart = 0;
                if (e.Source is ListBox target)
                {
                    if (target.ItemsSource is ObservableCollection<Game> cl)
                    {
                        targetItems = cl;
                        selected = target.SelectedItems;
                    }
                    if (e.OriginalSource is FrameworkElement item && item.DataContext is Game game)
                    {
                        var container = target.ItemContainerGenerator.ContainerFromItem(game) as FrameworkElement;
                        insertionStart = target.ItemContainerGenerator.IndexFromContainer(container);
                        var pos = e.GetPosition(container);
                        if (pos.Y >= container.ActualHeight / 2)
                        {
                            insertionStart += 1;
                        }
                    }
                } else if (e.Source is Expander expander)
                {
                    if (expander.Content is ListBox lb)
                    {
                        if (lb.ItemsSource is ObservableCollection<Game> cl)
                        {
                            targetItems = cl;
                            selected = lb.SelectedItems;
                        }
                    }
                }
                if (targetItems != null)
                {
                    selected.Clear();
                    int i = Math.Max(insertionStart, 0);
                    foreach (var game in games)
                    {
                        targetItems.Insert(i++, game);
                        selected.Add(game);
                    }

                }
                games.Clear();
            }
        }

        private class GameItemOption : Playnite.SDK.GenericItemOption
        {
            public Game Game { get; set; } = null;
            public GameItemOption(Game game)
            {
                Game = game;
                Name = game.Name;
                Description = $"Source: {game.Source?.Name ?? "Playnite"}, Platforms: {string.Join(", ", game.Platforms?.Select(p => p.Name)?? new List<string>())}";
            }
        }

        private void AddGamesButton_Click(object sender, RoutedEventArgs e)
        {
            //if (sender is Button bt && bt.DataContext is CustomGroupViewModel model)
            //{

            //    var options = DuplicateHiderPlugin.API.Database.Games.Select(g => new GameItemOption(g));

            //    var result = DuplicateHiderPlugin.API.Dialogs.ChooseItemWithSearch(
            //        (List<Playnite.SDK.GenericItemOption>)options,
            //        query => options.Where(opt => query.ToLower().Contains(query.ToLower())).Cast<Playnite.SDK.GenericItemOption>().ToList()
            //    );
            //    if (result is GameItemOption gameItemOption)
            //    {

            //    }
            //}
        }

        private void Expander_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus is ListBoxItem oldFocus && e.NewFocus is ListBoxItem newFocus)
            {
                if (newFocus.DataContext is Game game && e.Source is ListBox lb)
                {
                    if (!lb.ItemsSource.OfType<Game>().Contains(game))
                    {
                        lb.SelectedItems.Clear();
                    }
                }
            }
        }

        bool contextMenuOpen = false;

        private void GameList_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            contextMenuOpen = false;
        }

        private void GameList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            contextMenuOpen = true;
        }
    }
}
