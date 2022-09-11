using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DuplicateHider.ViewModels
{
    public class LibraryStatisticsViewModel : ObservableObject
    {
        public DuplicateHiderPlugin DuplicateHiderPlugin { get; private set; }

        private ObservableCollection<Models.LibraryStatisticsModel> libraries 
            = new ObservableCollection<Models.LibraryStatisticsModel>();
        public ObservableCollection<Models.LibraryStatisticsModel> Libraries { get => libraries; set => SetValue(ref libraries, value); }

        public CollectionView LibrariesCollection { get; }

        public LibraryStatisticsViewModel(DuplicateHiderPlugin plugin)
        {
            DuplicateHiderPlugin = plugin;
            Libraries = DuplicateHiderPlugin.PlayniteApi.Database.Sources.Select(s => new Models.LibraryStatisticsModel { LibrarySource = s })
                .Concat(new []{ new Models.LibraryStatisticsModel { LibrarySource = Constants.DEFAULT_SOURCE } })
                .Where(s => s.GamesTotal > 0)
                .OrderByDescending(m => m.GamesTotal).ToObservable();
            LibrariesCollection = new CollectionView(Libraries);
        }


    }
}
