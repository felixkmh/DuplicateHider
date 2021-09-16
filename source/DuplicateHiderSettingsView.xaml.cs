using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DuplicateHider
{
    public partial class DuplicateHiderSettingsView : UserControl
    {
        public DuplicateHiderSettingsView()
        {
            InitializeComponent();
        }

        public ListBoxItem CreatePriorityEntry(GameSource source)
        {
            Button buttonUp = new Button
            {
                Content = "▲",
                Margin = new Thickness(0, 0, 3, 0),
                Width = 20,
                Height = 20,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                ClipToBounds = false,
                Padding = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Arrow
            };
            buttonUp.Click += ButtonUp_Click;
            Button buttonDown = new Button
            {
                Content = "▼",
                Margin = new Thickness(0, 0, 8, 0),
                Width = 20,
                Height = 20,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                ClipToBounds = false,
                Padding = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Arrow
            };
            buttonDown.Click += ButtonDown_Click;
            Label label = new Label { Content = source != null ? source.Name : Constants.UNDEFINED_SOURCE };
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(buttonUp);
            stackPanel.Children.Add(buttonDown);
            stackPanel.Children.Add(label);
            ListBoxItem item = new ListBoxItem { Content = stackPanel, Tag = source, AllowDrop = true };
            item.Drop += Item_Drop;
            item.PreviewDragOver += Item_PreviewDragOver;
            // label.PreviewMouseLeftButtonDown += Label_PreviewMouseLeftButtonDown;
            item.PreviewMouseMove += Item_PreviewMouseMove;
            item.PreviewMouseLeftButtonDown += Item_MouseLeftButtonDown;
            item.PreviewMouseLeftButtonUp += Item_MouseLeftButtonUp;
            buttonUp.Tag = item;
            buttonDown.Tag = item;
            label.Tag = item;
            item.Cursor = Cursors.SizeNS;
            return item;
        }

        private void Item_Drop(object sender, DragEventArgs e)
        {
            PriorityListBox.SelectedItem = null;
        }

        bool dragging = false;
        Point draggingStart = new Point();

        private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            draggingStart = e.GetPosition(PriorityListBox);
        }

        private void Item_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem draggedItem)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var delta = (Vector)Point.Subtract(e.GetPosition(PriorityListBox), draggingStart);
                    if (dragging && delta.Length > 5)
                    {
                        DragDrop.DoDragDrop(draggedItem, draggedItem, DragDropEffects.Move);
                    }
                }
            }
        }

        private void Label_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label draggedItem)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem.Tag, DragDropEffects.Move);
            }
        }

        private void Item_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem hitItem)
            {
                ScrollViewer sv = (ScrollViewer)GetScrollViewer(PriorityListBox);
                if (sv != null)
                {
                    var relPos = e.GetPosition(PriorityListBox);
                    if (relPos.Y > PriorityListBox.ActualHeight - 30)
                    {
                        var delta = 30 - (PriorityListBox.ActualHeight - relPos.Y);
                        sv.ScrollToVerticalOffset(sv.VerticalOffset + delta * 0.3);
                    }
                    if (relPos.Y < 30)
                    {
                        var delta = 30 - relPos.Y;
                        sv.ScrollToVerticalOffset(sv.VerticalOffset - delta * 0.3);
                    }
                }
                if (e.Data.GetData(typeof(ListBoxItem)) is ListBoxItem droppedItem)
                {
                    int targetIdx = PriorityListBox.Items.IndexOf(hitItem);
                    int removedIdx = PriorityListBox.Items.IndexOf(droppedItem);
                    PriorityListBox.Items.RemoveAt(removedIdx);
                    PriorityListBox.Items.Insert(targetIdx, droppedItem);
                    PriorityListBox.SelectedItem = droppedItem;
                }
            }
        }

        // See https://stackoverflow.com/a/1009297
        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        private void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            var item = (ListBoxItem)((Button)sender).Tag;
            item.IsSelected = false;
            int index = PriorityListBox.Items.IndexOf(item);
            PriorityListBox.Items.RemoveAt(index);
            if (index < PriorityListBox.Items.Count - 1)
            {
                PriorityListBox.Items.Insert(index + 1, item);
            }
            else
            {
                PriorityListBox.Items.Add(item);
            }
        }

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            var item = (ListBoxItem)((Button)sender).Tag;
            int index = PriorityListBox.Items.IndexOf(item);
            if (index > 0)
            {
                PriorityListBox.Items.RemoveAt(index);
                PriorityListBox.Items.Insert(index - 1, item);
            }
        }

        public IEnumerable<CheckBox> Platforms { get; set; }
        public IEnumerable<CheckBox> Categories { get; set; }
        public IEnumerable<CheckBox> Sources { get; set; }

        private void PlatformComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Platforms.OrderByDescending(cb => cb.IsChecked).ThenBy(cb => (string)cb.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
                comboBox.IsDropDownOpen = true;
            }
        }

        private void SourceComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Sources.OrderByDescending(cb => cb.IsChecked).ThenBy(o => (string)o.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
                comboBox.IsDropDownOpen = true;
            }
        }

        private void CategoriesComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Categories.OrderByDescending(cb => cb.IsChecked).ThenBy(o => (string)o.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
                comboBox.IsDropDownOpen = true;
            }
        }

        private void PlatformComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Platforms.OrderByDescending(cb => cb.IsChecked).ThenBy(o => (string)o.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
            }
        }

        private void SourceComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Sources.OrderByDescending(cb => cb.IsChecked).ThenBy(o => (string)o.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
            }
        }

        private void CategoriesComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Items.Clear();
                foreach (var checkbox in Categories.OrderByDescending(cb => cb.IsChecked).ThenBy(o => (string)o.Content))
                {
                    bool found = ((string)checkbox.Content).ToLower().Contains(comboBox.Text.ToLower());
                    if (string.IsNullOrEmpty(comboBox.Text) || found)
                    {
                        comboBox.Items.Add(checkbox);
                    }
                }
            }
        }

        private void ReplacementRulesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListBox lb)
            {
                if (e.Key == Key.Delete)
                {
                    List<object> toDelete = new List<object>();
                    foreach (var item in lb.SelectedItems)
                    {
                        if (item is ListBoxItem lbi)
                        {
                            if (lbi.Tag as string != "empty")
                            {
                                toDelete.Add(lbi);
                            }
                        }
                    }
                    foreach (var item in toDelete)
                    {
                        lb.Items.Remove(item);
                    }
                }
            }
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewGroupNameBox.Text))
            {
                var groupName = NewGroupNameBox.Text;
                GroupsList.AddGroup(new Data.CustomGroup() { Name = groupName });
            }
        }
    }
}