using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DuplicateHider.Models
{
    public class LibraryStatisticsModel : ObservableObject
    {
        private GameSource librarySource;
        public GameSource LibrarySource 
        { 
            get => librarySource; 
            set 
            { 
                SetValue(ref librarySource, value); 
                OnPropertyChanged(nameof(Icon));
                OnPropertyChanged(nameof(GamesTotal));
                OnPropertyChanged(nameof(HiddenDuplicates));
            } 
        }

        public ImageSource Icon => DuplicateHiderPlugin.SourceIconCache.GetOrGenerate(new Game { SourceId = LibrarySource?.Id ?? Guid.Empty });

        public int GamesTotal => DuplicateHiderPlugin.API.Database.Games.Count(g => (g.Source ?? Constants.DEFAULT_SOURCE) == LibrarySource);

        public int HiddenDuplicates => DuplicateHiderPlugin.Instance.Index.Values
            .Sum(e => e.Skip(1).Count(g => (DuplicateHiderPlugin.API.Database.Games.Get(g).Source ?? Constants.DEFAULT_SOURCE) == LibrarySource));
    }
}
