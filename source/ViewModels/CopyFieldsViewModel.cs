using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DuplicateHider.Models;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DuplicateHider.ViewModels
{
    public class CopyFieldsViewModel : ObservableObject
    {
        protected IEnumerable<CopyFieldsModel> copyFields = Array.Empty<CopyFieldsModel>();
        public IEnumerable<CopyFieldsModel> CopyFields { get => copyFields; set { SetValue(ref copyFields, value); OnPropertyChanged(nameof(Sources)); } }

        protected EnabledFieldsModel enabledFields = DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields.Copy();
        public EnabledFieldsModel EnabledFields { get => enabledFields; set => SetValue(ref enabledFields, value); }

        public IEnumerable<Game> Sources => CopyFields.Select(cf => cf.SourceGame);

        protected bool saveAsDefault = false;
        public bool SaveAsDefault { get => saveAsDefault; set => SetValue(ref saveAsDefault, value); }

        public ICommand ApplyCommand { get; protected set; }
        public ICommand RevertCommand { get; protected set; }

        public CopyFieldsViewModel()
        {
            ApplyCommand = new RelayCommand(() =>
            {
                if (DuplicateHiderPlugin.API.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOC_DH_CopyFieldsWarning"), ResourceProvider.GetString("LOCOKLabel"), ResourceProvider.GetString("LOCCancelLabel")),
                    ResourceProvider.GetString("LOC_DH_Warning"), System.Windows.MessageBoxButton.OKCancel)
                == System.Windows.MessageBoxResult.OK)
                {
                    CopyFields.ForEach(cf => { cf.Apply(EnabledFields); });
                    DuplicateHiderPlugin.API.Database.Games.Update(CopyFields.SelectMany(cf => cf.TargetGames));
                    if (SaveAsDefault) DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields = EnabledFields;
                }
                GC.Collect();
            });
            RevertCommand = new RelayCommand(() =>
            {
                CopyFields.ForEach(cf => cf.Revert());
            });
        }

        public CopyFieldsViewModel(Game source, IEnumerable<Game> targets) : this()
        {
            copyFields = new CopyFieldsModel[] { new CopyFieldsModel(source, targets) };
        }

        public CopyFieldsViewModel(IEnumerable<CopyFieldsModel> copyFields) : this()
        {
            this.copyFields = copyFields;
        }

    }
}
