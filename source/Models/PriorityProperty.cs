using GongSolutions.Wpf.DragDrop;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DuplicateHider.Models
{
    public class PriorityProperty : ObservableObject, IComparer<Game>
    {
        private string propertyName = string.Empty;
        public string PropertyName { get => propertyName; set => SetValue(ref propertyName, value); }

        private ListSortDirection direction = ListSortDirection.Ascending;
        public ListSortDirection Direction { get => direction;
            set
            {
                var oldValue = Direction;
                if (oldValue != value)
                {
                    SetValue(ref direction, value);
                    OnPropertyChanged(nameof(IsAscending));
                }
            }
        }

        private ObservableCollection<string> priorityList = new ObservableCollection<string>();
        public ObservableCollection<string> PriorityList { get => priorityList; set => SetValue(ref priorityList, value); }

        public bool IsAscending
        {
            get => Direction == ListSortDirection.Ascending;
            set
            {
                var oldValue = IsAscending;
                if (oldValue != value)
                {
                    Direction = value ? ListSortDirection.Ascending : ListSortDirection.Descending;
                }
            }
        }

        public PriorityProperty()
        {

        }

        public PriorityProperty(string propertyName, IPlayniteAPI api)
        {
            PropertyName = propertyName;
            if (IsBool)
            {
                PriorityList.Add(true.ToString());
                PriorityList.Add(false.ToString());
            }

            var propertyInfo = typeof(Game).GetProperty(PropertyName);
            var type = propertyInfo.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type != typeof(DateTime) && type != typeof(int) && type != typeof(float) && type != typeof(double))
            {
                HashSet<string> set = new HashSet<string>();
                if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    foreach (var game in api.Database.Games)
                    {
                        var val = propertyInfo.GetValue(game);
                        if (val != null)
                        {
                            if (val is IEnumerable enumerable)
                            {
                                foreach (var item in enumerable)
                                {
                                    set.Add(item.ToString());
                                }
                            }
                        }
                    }
                } else
                {
                    foreach (var game in api.Database.Games)
                    {
                        var val = propertyInfo.GetValue(game);
                        if (val != null)
                        {
                            set.Add(val.ToString());
                        } else
                        {
                            set.Add(Activator.CreateInstance(type).ToString());
                        }
                    }
                }
                
                foreach (var item in set)
                {
                    PriorityList.AddMissing(item);
                }
            }
        }

        [JsonIgnore]
        public int DirectionMulitplier => Direction == ListSortDirection.Ascending ? 1 : -1;

        [JsonIgnore]
        public bool IsList 
        { 
            get
            {
                var type = typeof(Game).GetProperty(PropertyName).PropertyType;
                if (type == typeof(Guid)) return true;
                if (type.IsGenericType && type.GetGenericArguments()[0] == typeof(Guid)) return true;
                return IsBool;
            } 
        }
        [JsonIgnore]
        public bool IsBool
        {
            get
            {
                return typeof(Game).GetProperty(PropertyName).PropertyType == typeof(bool);
            }
        }
        [JsonIgnore]
        public bool IsComparable
        {
            get
            {
                if (IsBool || IsList) return false;
                System.Reflection.PropertyInfo propertyInfo = typeof(Game).GetProperty(PropertyName);
                var type = propertyInfo.PropertyType;
                var nullableType = Nullable.GetUnderlyingType(type);
                if (nullableType != null)
                {
                    type = nullableType;
                }
                var types = type.GetInterfaces();
                return types.Contains(typeof(IComparable));
            }
        }

        public int Compare(Game a, Game b)
        {
            var propertyInfo = typeof(Game).GetProperty(PropertyName);
            if (IsList)
            {
                var type = propertyInfo.PropertyType;
                type = Nullable.GetUnderlyingType(type) ?? type;
                if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                {
                    var valueA = propertyInfo.GetValue(a);
                    var valueB = propertyInfo.GetValue(b);
                    if (valueA is IEnumerable valuesA && valueB is IEnumerable valuesB)
                    {
                        var stringsA = valuesA.Cast<object>().Select(o => o.ToString()).ToList();
                        var stringsB = valuesB.Cast<object>().Select(o => o.ToString()).ToList();
                        if (stringsA.Count == 0 || stringsB.Count == 0)
                        {
                            return 0;
                        }
                        foreach (var item in valuesA)
                        {
                            PriorityList.AddMissing(item.ToString());
                        }
                        foreach (var item in valuesB)
                        {
                            PriorityList.AddMissing(item.ToString());
                        }
                        var minA = stringsA.Min(s => PriorityList.IndexOf(s));
                        var minB = stringsB.Min(s => PriorityList.IndexOf(s));
                        if (minA == minB)
                        {
                            minA = PriorityList.IndexOf(stringsA[0]);
                            minB = PriorityList.IndexOf(stringsB[0]);
                        }
                        return minA.CompareTo(minB);
                    }
                    else
                    {
                        var stringA = valueA.ToString();
                        var stringB = valueB.ToString();
                        PriorityList.AddMissing(stringA);
                        PriorityList.AddMissing(stringB);
                        return PriorityList.IndexOf(stringA).CompareTo(PriorityList.IndexOf(stringB));
                    }
                }
                else
                {
                    var valueA = propertyInfo.GetValue(a);
                    var valueB = propertyInfo.GetValue(b);
                    PriorityList.AddMissing(valueA?.ToString() ?? Activator.CreateInstance(type).ToString());
                    PriorityList.AddMissing(valueB?.ToString() ?? Activator.CreateInstance(type).ToString());
                    return PriorityList.IndexOf(valueA.ToString()).CompareTo(PriorityList.IndexOf(valueB.ToString()));
                }
                
            }
            if (IsBool)
            {
                var valueA = propertyInfo.GetValue(a).ToString();
                var valueB = propertyInfo.GetValue(b).ToString();
                return PriorityList.IndexOf(valueA).CompareTo(PriorityList.IndexOf(valueB));
            }
            if (IsComparable)
            {
                var valueA = (IComparable)propertyInfo.GetValue(a);
                var valueB = (IComparable)propertyInfo.GetValue(b);
                if (valueA == valueB)
                {
                    return 0;
                }
                if (valueA != null && valueB == null)
                {
                    return -1;
                }
                if (valueA == null && valueB != null)
                {
                    return 1;
                }
                return valueA.CompareTo(valueB) * DirectionMulitplier;
            }
            return 0;
        }
    }
}
