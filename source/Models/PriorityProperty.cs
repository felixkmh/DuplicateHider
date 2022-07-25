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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

        private Lazy<ObservableCollection<object>> priorityObjects;
        [JsonIgnore]
        public ObservableCollection<object> PriorityObjects { get => priorityObjects.Value; }

        private ObservableCollection<string> priorityList = new ObservableCollection<string>();
        public ObservableCollection<string> PriorityList { get => priorityObjects.IsValueCreated ? PriorityObjects.Select(o => o.ToString()).ToObservable() : priorityList; set => SetValue(ref priorityList, value); }

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
            prioritySet = new Lazy<HashSet<object>>(() =>
            {
                if (!IsList) PriorityList.Clear();
                return priorityObjects.Value.ToHashSet();
            });
            priorityObjects = new Lazy<ObservableCollection<object>>(() =>
            {
                if (IsList)
                {
                    var tryParseMethod = propertyType.Value.GetMethod("TryParse", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public);
                    if (isEnumerable.Value && propertyType.Value.IsGenericType && propertyType.Value.GenericTypeArguments.Length > 0)
                    {
                        var genericType = propertyType.Value.GenericTypeArguments[0];
                        tryParseMethod = genericType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public);
                    }
                    if (tryParseMethod != null)
                    {
                        return priorityList.Select(priority => {
                            object value = null;
                            object[] parameters = new object[] { priority, value };
                            tryParseMethod.Invoke(null, BindingFlags.Static, null, parameters, null);
                            return parameters[1];
                        }).Distinct().ToObservable();
                    }
                }
                return priorityList.Cast<object>().ToObservable();
            });
        }

        public PriorityProperty(string propertyName, IPlayniteAPI api) : this()
        {
            PropertyName = propertyName;
            if (IsBool)
            {
                AddValue(true);
                AddValue(false);
            }

            var type = propertyType.Value;
            if (type != typeof(DateTime) && !type.IsNumberType() && IsList)
            {
                HashSet<object> set = new HashSet<object>();
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
                                    set.Add(item);
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
                            set.Add(val);
                        } else if (propertyType.Value.IsValueType)
                        {
                            set.Add(Activator.CreateInstance(type));
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
                        isList = IsBool || PropertyName == nameof(Game.Version);
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
        private Lazy<HashSet<object>> prioritySet;

        private bool AddValue(object value)
        {
            if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    if (prioritySet.Value.Add(value))
                    {
                        PriorityObjects.Add(value);
                        return false;
                    }
                    return false;
                });
            }
            if (prioritySet.Value.Add(value))
            {
                PriorityObjects.Add(value);
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
                    var valuesA = (valueA as IEnumerable)?.Cast<object>().ToList();
                    var valuesB = (valueB as IEnumerable)?.Cast<object>().ToList();
                    //var stringsA = valuesA?.Cast<object>().Select(o => o.ToString()).ToList();
                    //var stringsB = valuesB?.Cast<object>().Select(o => o.ToString()).ToList();
                    if (valuesA != null && valuesA.Count > 0)
                    {
                        foreach (var item in valuesA)
                        {
                            AddValue(item);
                        }
                        minA = valuesA.Min(s => PriorityObjects.IndexOf(s));
                    }
                    if (valuesB != null && valuesB.Count > 0)
                    {
                        foreach (var item in valuesB)
                        {
                            AddValue(item);
                        }
                        minB = valuesB.Min(s => PriorityObjects.IndexOf(s));
                    }
                    if (minA == minB && minA != int.MaxValue)
                    {
                        minA = PriorityObjects.IndexOf(valuesA[0]);
                        minB = PriorityObjects.IndexOf(valuesB[0]);
                    }
                    return minA.CompareTo(minB);
                }
                else
                {
                    var valueA = propertyInfo.Value.GetValue(a);
                    var valueB = propertyInfo.Value.GetValue(b);
                    if (propertyType.Value.IsValueType)
                    {
                        AddValue(valueA ?? Activator.CreateInstance(propertyType.Value));
                        AddValue(valueB ?? Activator.CreateInstance(propertyType.Value));
                    } else
                    {
                        if (valueA == null && valueB == null) return 0;
                        if (valueA != null && valueB == null)
                        {
                            AddValue(valueA);
                            return -1;
                        }
                        if (valueA == null && valueB != null)
                        {
                            AddValue(valueB);
                            return 1;
                        }
                    }
                    return PriorityObjects.IndexOf(valueA).CompareTo(PriorityObjects.IndexOf(valueB));
                }
                
            }
            if (IsBool)
            {
                var valueA = propertyInfo.Value.GetValue(a);
                var valueB = propertyInfo.Value.GetValue(b);
                return PriorityObjects.IndexOf(valueA).CompareTo(PriorityObjects.IndexOf(valueB));
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
