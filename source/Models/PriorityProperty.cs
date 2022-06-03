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

        private Lazy<Type> propertyType;
        private Lazy<PropertyInfo> propertyInfo;

        private PropertyInfo InitPropertyInfo()
        {
            return typeof(Game).GetProperty(PropertyName);
        }

        private Type InitPropertyType()
        {
            var type = propertyInfo.Value.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type;

        }

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

        [JsonIgnore]
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
            propertyInfo = new Lazy<PropertyInfo>(InitPropertyInfo);
            propertyType = new Lazy<Type>(InitPropertyType);
            isEnumerable = new Lazy<bool>(() => propertyType.Value != typeof(string) && propertyType.Value.GetInterfaces().Contains(typeof(IEnumerable)));
            prioritySet = new Lazy<HashSet<string>>(() =>
            {
                if (!IsList) PriorityList.Clear();
                return PriorityList.ToHashSet();
            });
        }

        public PriorityProperty(string propertyName, IPlayniteAPI api) : this()
        {
            PropertyName = propertyName;
            if (IsBool)
            {
                AddValue(true.ToString());
                AddValue(false.ToString());
            }

            var type = propertyType.Value;
            if (type != typeof(DateTime) && type != typeof(int) && type != typeof(float) && type != typeof(double) && type != typeof(string))
            {
                HashSet<string> set = new HashSet<string>();
                if (isEnumerable.Value)
                {
                    foreach (var game in api.Database.Games)
                    {
                        var val = propertyInfo.Value.GetValue(game);
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
                        var val = propertyInfo.Value.GetValue(game);
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
                    AddValue(item);
                }
            }
        }

        [JsonIgnore]
        public int DirectionMulitplier => Direction == ListSortDirection.Ascending ? 1 : -1;

        private bool? isList = null;
        [JsonIgnore]
        public bool IsList 
        { 
            get
            {
                if (isList == null)
                {
                    isList = false;
                    var type = typeof(Game).GetProperty(PropertyName).PropertyType;
                    if (type == typeof(Guid))
                    {
                        isList = true;
                    } else if (type.IsGenericType && type.GetGenericArguments()[0] == typeof(Guid))
                    {
                        isList = true;
                    } else
                    {
                        isList = IsBool;
                    } 
                }
                return isList ?? default;
            } 
        }

        private bool? isBool = null;
        [JsonIgnore]
        public bool IsBool
        {
            get
            {
                if (isBool == null)
                {
                    isBool = typeof(Game).GetProperty(PropertyName).PropertyType == typeof(bool);
                }
                return isBool ?? default;
            }
        }

        private bool? isComparable = null;
        [JsonIgnore]
        public bool IsComparable
        {
            get
            {
                if (isComparable == null)
                {
                    isComparable = false;
                    if (IsBool || IsList)
                    {
                        isComparable = false;
                    } else
                    {
                        System.Reflection.PropertyInfo propertyInfo = typeof(Game).GetProperty(PropertyName);
                        var type = propertyInfo.PropertyType;
                        var nullableType = Nullable.GetUnderlyingType(type);
                        if (nullableType != null)
                        {
                            type = nullableType;
                        }
                        var types = type.GetInterfaces();
                        isComparable = types.Contains(typeof(IComparable));
                    }
                }
                return isComparable ?? default;
            }
        }

        private Lazy<bool> isEnumerable;
        private Lazy<HashSet<string>> prioritySet;

        private bool AddValue(string value)
        {
            if (prioritySet.Value.Add(value))
            {
                PriorityList.Add(value);
                return false;
            }
            return false;
        }

        public int Compare(Game a, Game b)
        {
            if (IsList)
            {
                var type = propertyType.Value;
                if (isEnumerable.Value)
                {
                    var valueA = propertyInfo.Value.GetValue(a);
                    var valueB = propertyInfo.Value.GetValue(b);
                    int minA = int.MaxValue;
                    int minB = int.MaxValue;
                    var valuesA = valueA as IEnumerable;
                    var valuesB = valueA as IEnumerable;
                    var stringsA = valuesA?.Cast<object>().Select(o => o.ToString()).ToList();
                    var stringsB = valuesB?.Cast<object>().Select(o => o.ToString()).ToList();
                    if (stringsA != null && stringsA.Count > 0)
                    {
                        foreach (var item in stringsA)
                        {
                            AddValue(item);
                        }
                        minA = stringsA.Min(s => PriorityList.IndexOf(s));
                    }
                    if (stringsB != null && stringsB.Count > 0)
                    {
                        foreach (var item in stringsB)
                        {
                            AddValue(item);
                        }
                        minB = stringsB.Min(s => PriorityList.IndexOf(s));
                    }
                    if (minA == minB && minA != int.MaxValue)
                    {
                        minA = PriorityList.IndexOf(stringsA[0]);
                        minB = PriorityList.IndexOf(stringsB[0]);
                    }
                    return minA.CompareTo(minB);
                }
                else
                {
                    var valueA = propertyInfo.Value.GetValue(a);
                    var valueB = propertyInfo.Value.GetValue(b);
                    AddValue(valueA?.ToString() ?? Activator.CreateInstance(type).ToString());
                    AddValue(valueB?.ToString() ?? Activator.CreateInstance(type).ToString());
                    return PriorityList.IndexOf(valueA.ToString()).CompareTo(PriorityList.IndexOf(valueB.ToString()));
                }
                
            }
            if (IsBool)
            {
                var valueA = propertyInfo.Value.GetValue(a).ToString();
                var valueB = propertyInfo.Value.GetValue(b).ToString();
                return PriorityList.IndexOf(valueA).CompareTo(PriorityList.IndexOf(valueB));
            }
            if (IsComparable)
            {
                var valueA = (IComparable)propertyInfo.Value.GetValue(a);
                var valueB = (IComparable)propertyInfo.Value.GetValue(b);
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
