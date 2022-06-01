using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DuplicateHider.Models;
using DuplicateHider.Converters;
using Playnite.SDK.Models;
using System.Windows.Data;
using Playnite.SDK;

namespace DuplicateHider.ViewModels
{
    public class PriorityPropertyViewModel : ObservableObject
    {
        private PriorityProperty priorityProperty;
        public PriorityProperty PriorityProperty { get => priorityProperty; set => SetValue(ref priorityProperty,value); }

        private PropertyInfo propertyInfo;

        public PriorityPropertyViewModel(PriorityProperty priorityProperty, IPlayniteAPI playniteAPI)
        {
            this.priorityProperty = priorityProperty;
            var gameType = typeof(Game);
            if (gameType.GetProperty(priorityProperty.PropertyName) is PropertyInfo info)
            {
                propertyInfo = info;
            }
        }


    }
}
