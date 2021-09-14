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

        internal ConcurrentDictionary<string, ImageSource> cache = new ConcurrentDictionary<string, ImageSource>();

        internal BitmapImage generate(Game game)
        {
            foreach (var path in GetSourceIconPaths(game))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = new Uri(path);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                } catch (Exception) {}
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
            var platform = game.Platforms?.FirstOrDefault()?.Name ?? Constants.UNDEFINED_PLATFORM;
            var key = source + platform;
            if (cache.TryGetValue(key, out var icon))
            {
                return icon;
            } else
            {
                var newIcon = generate(game);
                cache[key] = newIcon;
                return newIcon;
            }
        }

        private static readonly Dictionary<Playnite.SDK.BuiltinExtension, string> builtinToName = new Dictionary<Playnite.SDK.BuiltinExtension, string>()
        {
            {Playnite.SDK.BuiltinExtension.AmazonGamesLibrary, "amazon"          },
            {Playnite.SDK.BuiltinExtension.BattleNetLibrary,   "battle.net"      },
            {Playnite.SDK.BuiltinExtension.BethesdaLibrary,    "bethesda"        },
            {Playnite.SDK.BuiltinExtension.EpicLibrary,        "epic"            },
            {Playnite.SDK.BuiltinExtension.GogLibrary,         "gog"             },
            {Playnite.SDK.BuiltinExtension.HumbleLibrary,      "humble"          },
            {Playnite.SDK.BuiltinExtension.ItchioLibrary,      "itchio"          },
            {Playnite.SDK.BuiltinExtension.OriginLibrary,      "origin"          },
            {Playnite.SDK.BuiltinExtension.PSNLibrary,         "psn"             },
            {Playnite.SDK.BuiltinExtension.SteamLibrary,       "steam"           },
            {Playnite.SDK.BuiltinExtension.TwitchLibrary,      "twitch"          },
            {Playnite.SDK.BuiltinExtension.UplayLibrary,       "ubisoft connect" },
            {Playnite.SDK.BuiltinExtension.XboxLibrary,        "xbox"            },
        };
        

        internal static string GetResourceIconUri(Game game)
        {
            if (game is Game)
            {
                var pluginId = game.PluginId;
                var source = game.Source?.Name ?? Constants.UNDEFINED_SOURCE;
                if (builtinToName.TryGetValue(Playnite.SDK.BuiltinExtensions.GetExtensionFromId(pluginId), out var builtinSource))
                {
                    source = builtinSource;
                }
                source = Uri.EscapeDataString(source);
                var names = GetResourceNames()
                    .Where(n => n.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
                var name = names.FirstOrDefault(n => System.IO.Path.GetFileName(n).StartsWith(source, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(name))
                {
                    return $"pack://application:,,,/DuplicateHider;component/{name}";
                }
            }
            return null;
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

        protected IEnumerable<string> GetSourceIconPaths(Game game)
        {
            var name = game.Source != null ? game.Source.Name : Constants.UNDEFINED_SOURCE;
            bool enableThemeIcons = DuplicateHiderPlugin.Instance.settings.EnableThemeIcons;
            bool preferUserIcons = DuplicateHiderPlugin.Instance.settings.PreferUserIcons;

            List<string> paths = new List<string>();

            var userIconPath = GetUserIconPath(game);
            var themeIconPath = enableThemeIcons ? GetThemeIconPath(game) : null;
            var resourceIconPath = GetResourceIconUri(game);
            var pluginIconPath = GetPluginIconPath(game);
            var platformIconPath = GetPlatformIconPath(game);
            if (preferUserIcons) paths.Add(userIconPath);
            if (enableThemeIcons) paths.Add(themeIconPath);
            if (game.Source == null) paths.Add(platformIconPath);
            paths.Add(resourceIconPath);
            if (!preferUserIcons) paths.Add(userIconPath);
            paths.Add(pluginIconPath);
            if (game.Source != null) paths.Add(platformIconPath);

            return paths.Where(p => !string.IsNullOrEmpty(p) && Uri.TryCreate(p, UriKind.RelativeOrAbsolute, out var _))
                        .Concat(GetDefaultIconPaths());
        }

        private string GetPlatformIconPath(Game game)
        {
            var path = game?.Platforms?.FirstOrDefault()?.Icon;
            if (!string.IsNullOrEmpty(path))
            {
                if (!System.IO.Path.IsPathRooted(path))
                {
                    path = DuplicateHiderPlugin.API.Database.GetFullFilePath(path);
                }
                return path;
            }
            return null;
        }

        private static string GetThemeIconPath(Game game)
        {
            var sourceName = game?.PluginId.ToString() ?? "Default";
            if (builtinToName.TryGetValue(Playnite.SDK.BuiltinExtensions.GetExtensionFromId(game?.PluginId ?? Guid.Empty), out var builtinSource))
            {
                sourceName = builtinSource;
            }
            // sourceName = Uri.EscapeDataString(sourceName);
            if (DuplicateHiderPlugin.API.Resources.GetResource($"DuplicateHider_{sourceName}_Icon") is BitmapImage img)
            {
                return img.UriSource.ToString();
            }
            if (DuplicateHiderPlugin.API.Resources.GetResource($"DuplicateHider_{sourceName.ToLower()}_Icon") is BitmapImage img2)
            {
                return img2.UriSource.ToString();
            }
            if (DuplicateHiderPlugin.API.Resources.GetResource($"DuplicateHider_{sourceName.Capitalize()}_Icon") is BitmapImage img3)
            {
                return img3.UriSource.ToString();
            }
            return null;
        }

        private string GetUserIconPath(Game game)
        {
            var sourceName = game?.Source?.Name ?? "Default";
            if (UserIconFolderPaths.Count == 0) UserIconFolderPaths.Add(DuplicateHiderPlugin.Instance.GetUserIconFolderPath());
            var paths = UserIconFolderPaths
               .SelectMany(s => System.IO.Directory.GetFiles(s))
               .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(sourceName, StringComparison.OrdinalIgnoreCase))
               .FirstOrDefault(f =>
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
            if ((paths?.Count() ?? 0) == 0)
            {
                if (builtinToName.TryGetValue(Playnite.SDK.BuiltinExtensions.GetExtensionFromId(game?.PluginId ?? Guid.Empty), out var builtinSource))
                {
                    sourceName = builtinSource;
                    paths = UserIconFolderPaths
                        .SelectMany(s => System.IO.Directory.GetFiles(s))
                        .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals(sourceName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault(f =>
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));
                }
            }
            return paths;
        }

        protected IEnumerable<string> GetDefaultIconPaths()
        {
            var name = "Default";
            bool enableThemeIcons = DuplicateHiderPlugin.Instance.settings.EnableThemeIcons;
            bool preferUserIcons = DuplicateHiderPlugin.Instance.settings.PreferUserIcons;

            List<string> paths = new List<string>();

            var userIconPath = GetUserIconPath(null);
            var themeIconPath = enableThemeIcons ? GetThemeIconPath(null) : null;
            var resourceIconPath = GetResourceIconUri(null);
            if (preferUserIcons) paths.Add(userIconPath);
            if (enableThemeIcons) paths.Add(themeIconPath);
            paths.Add(resourceIconPath);
            if (!preferUserIcons) paths.Add(userIconPath);

            paths.Add("pack://application:,,,/DuplicateHider;component/icons/playnite.ico");

            return paths.Where(p => !string.IsNullOrEmpty(p) && Uri.TryCreate(p, UriKind.RelativeOrAbsolute, out var _));
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
                    if (path is string && !System.IO.Path.IsPathRooted(path))
                    {
                        var configPath = DuplicateHiderPlugin.API.Paths.ConfigurationPath;
                        path = System.IO.Path.Combine(configPath, "Extensions", plugin.Id.ToString(), path);
                    }
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
