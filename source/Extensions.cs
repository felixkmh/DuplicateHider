using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DuplicateHider
{
    public static class Extensions
    {
        public static int CompareTo<T>(this T a, T b, IEnumerable<IComparer<T>> comparers)
        {
            if (comparers != null)
            {
                foreach(var comparer in comparers)
                {
                    var c = comparer.Compare(a, b);
                    if (c != 0)
                    {
                        return c;
                    }
                }
            }
            return 0;
        }

        public static string Capitalize(this string input)
        {
            var builder = new StringBuilder();
            bool lastCharSpace = true;
            foreach (var c in input)
            {
                if (lastCharSpace)
                {
                    builder.Append(char.ToUpper(c));
                } else
                {
                    builder.Append(c);
                }
                lastCharSpace = c == ' ' || c == '\t';
            }
            return builder.ToString();
        }

        public static bool InsertSorted<T>(this IList<T> list, T item, Func<T, int> toValue)
        {
            if (list.Contains(item))
            {
                return false;
            }

            int i;
            for (i = 0; i < list.Count; ++i)
            {
                if (i < list.Count && toValue(item) <= toValue(list[i]))
                {
                    break;
                }
            }

            list.Insert(i, item);
            return true;
        }

        public static bool InsertSorted<T>(this IList<T> list, T item, Func<T, T, int> compare)
        {
            if (list.Contains(item))
            {
                return false;
            }

            int i;
            for (i = 0; i < list.Count; ++i)
            {
                if (i < list.Count && compare(item, list[i]) <= 0)
                {
                    break;
                }
            }

            list.Insert(i, item);
            return true;
        }

        public static bool InsertSorted<T>(this IList<T> list, T item, IComparer<T> comparer)
        {
            if (list.Contains(item))
            {
                return false;
            }

            int i;
            for (i = 0; i < list.Count; ++i)
            {
                if (comparer.Compare(item, list[i]) <= 0)
                {
                    break;
                }
            }

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
                return Constants.UNDEFINED_SOURCE;
            }
            else
            {
                return game.Source.Name;
            }
        }

        public static string GetPlatformName(this Game game)
        {
            if (game.Platforms?.FirstOrDefault() == null)
            {
                return Constants.UNDEFINED_SOURCE;
            }
            else
            {
                return game.Platforms?.FirstOrDefault().Name;
            }
        }

        public static IEnumerable<string> GetPlatformNames(this Game game)
        {
            if (game.Platforms?.FirstOrDefault() == null)
            {
                return new[] { Constants.UNDEFINED_SOURCE };
            }
            else
            {
                return game.Platforms?.Select(p => p.Name);
            }
        }

        public static IEnumerable<string> GetCategories(this Game game)
        {
            if (game.Categories == null)
            {
                return Array.Empty<string>();
            }
            else
            {
                return from cat in game.Categories select cat.Name;
            }
        }

        public static bool TryFind<T>(this IEnumerable<T> items, Predicate<T> predicate, out T result)
        {
            foreach (var item in items)
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
            if (instance == null)
            {
                return new T();
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(instance));
        }

        public static string Suffix(this string name, int i)
        {
            return i == 0 ? name : (name + i.ToString());
        }
    }
}
