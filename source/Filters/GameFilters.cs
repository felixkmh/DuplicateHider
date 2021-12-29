using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DuplicateHider
{
    class PlaceboGameFilter : IFilter<IEnumerable<Game>>
    {
        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return input;
        }
    }

    class PlatformFilter : IFilter<IEnumerable<Game>>
    {
        protected bool _include;
        protected IEnumerable<string> _platforms;

        public PlatformFilter(bool include, IEnumerable<string> platforms)
        {
            _include = include;
            _platforms = platforms;
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return
                from game
                in input
                where _include == _platforms.Any(p => game.GetPlatformNames().Any(name => name == p))
                select game;
        }
    }

    class SourceFilter : IFilter<IEnumerable<Game>>
    {
        protected bool _include;
        protected IEnumerable<string> _sources;

        public SourceFilter(bool include, IEnumerable<string> sources)
        {
            _include = include;
            _sources = sources;
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return
                from game
                in input
                where _include == _sources.Contains(game.GetSourceName())
                select game;
        }
    }

    class CategoryFilter : IFilter<IEnumerable<Game>>
    {
        protected bool _include;
        protected IEnumerable<string> _categories;

        public CategoryFilter(bool include, IEnumerable<string> categories)
        {
            _include = include;
            _categories = categories;
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return
                from game
                in input
                where _include == game.GetCategories().Any(cat => _categories.Contains(cat))
                select game;
        }
    }

    class IgnoreFilter : IFilter<IEnumerable<Game>>
    {
        ISet<Guid> _ignoredIds;
        public IgnoreFilter(ISet<Guid> ignoredIds)
        {
            _ignoredIds = ignoredIds;
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return from game in input where !_ignoredIds.Contains(game.Id) select game;
        }
    }

    class UnionFilter : IFilter<IEnumerable<Game>>
    {
        ISet<Game> _addedGames;

        public UnionFilter(IEnumerable<Game> games)
        {
            _addedGames = games.ToHashSet();
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return input.Union(_addedGames);
        }
    }

    class NameNullFilter : IFilter<IEnumerable<Game>>
    {
        public NameNullFilter()
        {
            
        }

        public override IEnumerable<Game> ApplySingle(in IEnumerable<Game> input)
        {
            return input.Where(g => g.Name != null);
        }
    }
}
