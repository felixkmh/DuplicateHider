using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider
{
    public static class Extensions
    {
        public static bool InsertSorted<T>(this IList<T> list, T item, Func<T, int> toValue)
        {
            if (list.Contains(item)) return false;
            int i;
            for (i = 0; i < list.Count; ++i)
                if (i < list.Count && toValue(item) <= toValue(list[i]))
                    break;
            list.Insert(i, item);
            return true;
        }

        public static bool InsertSorted<T>(this IList<T> list, T item, Comparer<T> comparer)
        {
            if (list.Contains(item)) return false;
            int i;
            for (i = 0; i < list.Count; ++i)
                if (comparer.Compare(item, list[i]) <= 0)
                    break;
            list.Insert(i, item);
            return true;
        }

        public static bool InsertSorted<T>(this IList<T> list, T item) where T : IComparer<T>
        {
            return list.InsertSorted<T>(item, Comparer<T>.Default);
        }

        public static string GetSourceName(this Game game)
        {
            if (game.Source == null)
            {
                return "Undefined";
            }
            else
            {
                return game.Source.Name;
            }
        }

        public static string GetPlatformName(this Game game)
        {
            if (game.Platform == null)
            {
                return "Undefined";
            }
            else
            {
                return game.Platform.Name;
            }
        }

        public static IEnumerable<string> GetCategories(this Game game)
        {
            if (game.Categories == null)
            {
                return new string[]{ };
            }
            else
            {
                return from cat in game.Categories select cat.Name;
            }
        }

        public static bool TryFind<T>(this IEnumerable<T> items, Predicate<T> predicate, out T result)
        {
            foreach(var item in items)
            {
                if (predicate(item))
                {
                    result = item;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public static T Copy<T>(this T instance) where T : new()
        {
            if (Object.ReferenceEquals(instance, null))
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(instance));
        }
    }
}
