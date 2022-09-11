using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DuplicateHider.ViewModels
{
    public class AddTagViewModel : ObservableObject
    {
        public ObservableCollection<Guid> TagIds { get; set; }
        public ObservableCollection<Tag> AvailableTags => Playnite.SDK.API.Instance.Database.Tags.ToObservable();
        private string filterText;
        public string FilterText { get => filterText; set { SetValue(ref filterText, value?.ToLower()); UpdateFilter(); }  }

        private void UpdateFilter()
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                AvailableTagsView.Filter = _ => true;
            } else
            {
                AvailableTagsView.Filter = arg =>
                {
                    if (arg is Tag tag)
                    {
                        return tag.Name?.ToLower().Contains(FilterText) ?? false;
                    }
                    return false;
                };
            }
        }

        public ICollectionView AvailableTagsView { get; }
        public ICommand AddTagsCommand { get; }

        public AddTagViewModel(ObservableCollection<Guid> guids)
        {
            TagIds = guids;
            AddTagsCommand = new RelayCommand<IList>(args =>
            {
                foreach (var tag in args.OfType<Tag>())
                {
                    TagIds.AddMissing(tag.Id);
                }
            });
            AvailableTagsView = new ListCollectionView(AvailableTags);
        }
    }
}
