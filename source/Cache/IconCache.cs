using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DuplicateHider.Cache
{
    class IconCache : IGeneratorCache<ImageSource, Game>, IReadOnlyDictionary<GameSource, ImageSource>
    {
        public List<string> UserIconFolderPaths { get; set; } = new List<string>();

        public IEnumerable<GameSource> Keys => ((IReadOnlyDictionary<GameSource, ImageSource>)cache).Keys;

        public IEnumerable<ImageSource> Values => ((IReadOnlyDictionary<GameSource, ImageSource>)cache).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<GameSource, ImageSource>>)cache).Count;

        public ImageSource this[GameSource key] => ((IReadOnlyDictionary<GameSource, ImageSource>)cache)[key];

        internal ConcurrentDictionary<GameSource, ImageSource> cache = new ConcurrentDictionary<GameSource, ImageSource>();

        internal BitmapImage generate(Game game)
        {

            if (GetSourceIconPath(game) is string path)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            return default(BitmapImage);
        }

        public void Clear()
        {
            cache.Clear();
        }

        public ImageSource GetOrGenerate(Game game)
        {
            if (game == null) return null;
            var source = game.Source ?? Constants.DEFAULT_SOURCE;
            if (cache.TryGetValue(source, out var icon))
            {
                return icon;
            } else
            {
                var newIcon = generate(game);
                cache[source] = newIcon;
                return newIcon;
            }
        }

        internal static string GetResourceIconUri(string sourceName)
        {
            var source = Uri.EscapeDataString(sourceName);
            var name = GetResourceNames()
                .Where(n => n.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(n => System.IO.Path.GetFileName(n).StartsWith(source, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(name))
            {
                return $"pack://application:,,,/DuplicateHider;component/{name}";
            }
            else
            {
                return null;
            }
        }

        // https://stackoverflow.com/a/2517799
        internal static string[] GetResourceNames()
        {
            var asm = Assembly.GetAssembly(typeof(DuplicateHiderPlugin));
            string resName = asm.GetName().Name + ".g.resources";
            using (var stream = asm.GetManifestResourceStream(resName))
            using (var reader = new System.Resources.ResourceReader(stream))
            {
                return reader.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
            }
        }

        protected string GetSourceIconPath(Game game)
        {
            var name = game.Source != null ? game.Source.Name : Constants.UNDEFINED_SOURCE;
            bool enableThemeIcons = DuplicateHiderPlugin.DHP.settings.EnableThemeIcons;
            bool preferUserIcons = DuplicateHiderPlugin.DHP.settings.PreferUserIcons;

            List<string> paths = new List<string>();

            var userIconPath = GetUserIconPath(name);
            var themeIconPath = enableThemeIcons ? GetThemeIconPath(name) : null;
            var resourceIconPath = GetResourceIconUri(name);
            var pluginIconPath = GetPluginIconPath(game);
            if (preferUserIcons) paths.Add(userIconPath);
            if (enableThemeIcons) paths.Add(themeIconPath);
            paths.Add(resourceIconPath);
            if (!preferUserIcons) paths.Add(userIconPath);
            paths.Add(pluginIconPath);

            var path = paths.FirstOrDefault(p => !string.IsNullOrEmpty(p));

            return Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var _)
                ? path
                : GetDefaultIconPath();
        }

        private static string GetThemeIconPath(string sourceName)
        {
            if (DuplicateHiderPlugin.API.Resources.GetResource($"DuplicateHider_{sourceName}_Icon") is BitmapImage img)
            {
                return img.UriSource.ToString();
            }
            return null;
        }

        private string GetUserIconPath(string sourceName)
        {
            return UserIconFolderPaths
               .SelectMany(s => System.IO.Directory.GetFiles(s))
               .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(sourceName, StringComparison.OrdinalIgnoreCase))
               .FirstOrDefault(f =>
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
        }

        protected string GetDefaultIconPath()
        {
            var name = "Default";
            bool enableThemeIcons = DuplicateHiderPlugin.DHP.settings.EnableThemeIcons;
            bool preferUserIcons = DuplicateHiderPlugin.DHP.settings.PreferUserIcons;

            List<string> paths = new List<string>();

            var userIconPath = GetUserIconPath(name);
            var themeIconPath = enableThemeIcons ? GetThemeIconPath(name) : null;
            var resourceIconPath = GetResourceIconUri(name);
            if (preferUserIcons) paths.Add(userIconPath);
            if (enableThemeIcons) paths.Add(themeIconPath);
            paths.Add(resourceIconPath);
            if (!preferUserIcons) paths.Add(userIconPath);

            var path = paths.FirstOrDefault(p => !string.IsNullOrEmpty(p));
            return Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var _)
                ? path
                : "pack://application:,,,/DuplicateHider;component/icons/undefined.ico";
        }

        protected string GetPluginIconPath(Game game)
        {
            if (game.PluginId is Guid id)
            {
                var plugin = DuplicateHiderPlugin.API.Addons.Plugins
                    .OfType<Playnite.SDK.Plugins.LibraryPlugin>()
                    .FirstOrDefault(p => p.Id == id);

                if (plugin is Playnite.SDK.Plugins.LibraryPlugin lp)
                {
                    var path = lp.LibraryIcon;
                    return path;
                }
            }

            return null;
        }

        public bool ContainsKey(GameSource key)
        {
            return ((IReadOnlyDictionary<GameSource, ImageSource>)cache).ContainsKey(key);
        }

        public bool TryGetValue(GameSource key, out ImageSource value)
        {
            return ((IReadOnlyDictionary<GameSource, ImageSource>)cache).TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<GameSource, ImageSource>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<GameSource, ImageSource>>)cache).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)cache).GetEnumerator();
        }
    }
}
