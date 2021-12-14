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
        public IEnumerable<CopyFieldsModel> CopyFields { get => copyFields; set => SetValue(ref copyFields, value); }

        protected EnabledFieldsModel enabledFields = DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields.Copy();
        public EnabledFieldsModel EnabledFields { get => enabledFields; set => SetValue(ref enabledFields, value); }

        protected bool saveAsDefault = false;
        public bool SaveAsDefault { get => saveAsDefault; set => SetValue(ref saveAsDefault, value); }

        public ICommand ApplyCommand { get; protected set; }
        public ICommand RevertCommand { get; protected set; }

        public CopyFieldsViewModel()
        {
            ApplyCommand = new RelayCommand(() =>
            {
                CopyFields.ForEach(cf => cf.Apply(EnabledFields));
                if (SaveAsDefault) DuplicateHiderPlugin.Instance.settings.DefaultEnabledFields = EnabledFields;
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
